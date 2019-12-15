using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Medallion.Collections
{
    public static partial class EqualityComparers
    {
        /// <summary>
        /// Gets an <see cref="EqualityComparer{T}"/> that compares instances of <see cref="IEnumerable{TElement}"/> such
        /// that two sequences with the same elements (but not necessarily the same order) are considered equal.
        /// The optional <paramref name="elementComparer"/> can be used to override the comparison of individual elements
        /// </summary>
        public static EqualityComparer<IEnumerable<TElement>> GetEnumerableEquivalenceComparer<TElement>(IEqualityComparer<TElement>? elementComparer = null)
        {
            return elementComparer == null || elementComparer == EqualityComparer<TElement>.Default
                ? EnumerableEquivalenceEqualityComparer<TElement>.DefaultInstance
                : new EnumerableEquivalenceEqualityComparer<TElement>(elementComparer);
        }

        private sealed class EnumerableEquivalenceEqualityComparer<TElement> : EqualityComparer<IEnumerable<TElement>>
        {
            private static EqualityComparer<IEnumerable<TElement>>? defaultInstance;
            public static EqualityComparer<IEnumerable<TElement>> DefaultInstance => defaultInstance ??= new EnumerableEquivalenceEqualityComparer<TElement>(EqualityComparer<TElement>.Default);

            private readonly IEqualityComparer<TElement> elementComparer;

            public EnumerableEquivalenceEqualityComparer(IEqualityComparer<TElement> elementComparer)
            {
                this.elementComparer = elementComparer;
            }

            public override bool Equals(IEnumerable<TElement> x, IEnumerable<TElement> y)
            {
                if (x == y) { return true; }
                if (x == null || y == null) { return false; }

                return AreEquivalent(x, y, this.elementComparer);
            }

            public override int GetHashCode(IEnumerable<TElement> obj) => obj == null ? 0 : ComputeOrderAgnosticHash(obj, this.elementComparer);

            public override bool Equals(object obj) => obj is EnumerableEquivalenceEqualityComparer<TElement> that && that.elementComparer.Equals(this.elementComparer);

            // including this.GetType() so that we don't hash-collide with the underlying comparer
            public override int GetHashCode() => HashHelper.GetHashCode(this.GetType(), this.elementComparer.GetHashCode());

            private static bool AreEquivalent(IEnumerable<TElement> @this, IEnumerable<TElement> that, IEqualityComparer<TElement> comparer)
            {
                // FastCount optimization: If both of the collections are materialized and have counts, 
                // we can exit very quickly if those counts differ
                var hasThisCount = TryFastCount(@this, out var thisCount);
                bool hasThatCount;
                if (hasThisCount)
                {
                    hasThatCount = TryFastCount(that, out var thatCount);
                    if (hasThatCount)
                    {
                        if (thisCount != thatCount)
                        {
                            return false;
                        }
                        if (thisCount == 0)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    hasThatCount = false;
                }

                var itemsEnumerated = 0;
                // SequenceEqual optimization: we reduce/avoid hashing
                // the collections have common prefixes, at the cost of only one
                // extra Equals() call in the case where the prefixes are not common
                using var thisEnumerator = @this.GetEnumerator();
                using var thatEnumerator = that.GetEnumerator();
                while (true)
                {
                    var thisFinished = !thisEnumerator.MoveNext();
                    var thatFinished = !thatEnumerator.MoveNext();

                    if (thisFinished)
                    {
                        // either this shorter than that, or the two were sequence-equal
                        return thatFinished;
                    }
                    if (thatFinished)
                    {
                        // that shorter than this
                        return false;
                    }

                    // keep track of this so that we can factor it into count-based 
                    // logic below
                    ++itemsEnumerated;

                    if (!comparer.Equals(thisEnumerator.Current, thatEnumerator.Current))
                    {
                        break; // prefixes were not equal
                    }
                }

                // now, build a dictionary of item => count out of one collection and then
                // probe it with the other collection to look for mismatches

                // Build/Probe Choice optimization: if we know the count of one collection, we should
                // use the other collection to build the dictionary. That way we can bail immediately if
                // we see too few or too many items
                CountingSet? elementCounts;
                IEnumerator<TElement> probeSide;
                if (hasThisCount)
                {
                    // we know this's count => use that as the build side
                    probeSide = thisEnumerator;
                    var remaining = thisCount - itemsEnumerated;
                    if (hasThatCount)
                    {
                        // if we have both counts, that means they must be equal or we would have already
                        // exited. However, in this case, we know exactly the capacity needed for the dictionary
                        // so we can avoid resizing
                        elementCounts = new CountingSet(comparer, capacity: remaining);
                        do
                        {
                            elementCounts.Increment(thatEnumerator.Current);
                        }
                        while (thatEnumerator.MoveNext());
                    }
                    else
                    {
                        elementCounts = TryBuildElementCountsWithKnownCount(thatEnumerator, remaining, comparer);
                    }
                }
                else if (TryFastCount(that, out var thatCount))
                {
                    // we know that's count => use this as the build side
                    probeSide = thatEnumerator;
                    var remaining = thatCount - itemsEnumerated;
                    elementCounts = TryBuildElementCountsWithKnownCount(thisEnumerator, remaining, comparer);
                }
                else
                {
                    // when we don't know either count, just use that as the build side arbitrarily
                    probeSide = thisEnumerator;
                    elementCounts = new CountingSet(comparer);
                    do
                    {
                        elementCounts.Increment(thatEnumerator.Current);
                    }
                    while (thatEnumerator.MoveNext());
                }

                // check whether we failed to construct a dictionary. This happens when we know
                // one of the counts and we detect, during construction, that the counts are unequal
                if (elementCounts == null)
                {
                    return false;
                }

                // probe the dictionary with the probe side enumerator
                do
                {
                    if (!elementCounts.TryDecrement(probeSide.Current))
                    {
                        // element in probe not in build => not equal
                        return false;
                    }
                }
                while (probeSide.MoveNext());

                // we are equal only if the loop above completely cleared out the dictionary
                return elementCounts.IsEmpty;
            }

            /// <summary>
            /// Constructs a count dictionary, staying mindful of the known number of elements
            /// so that we bail early (returning null) if we detect a count mismatch
            /// </summary>
            private static CountingSet? TryBuildElementCountsWithKnownCount(
                IEnumerator<TElement> elements,
                int remaining,
                IEqualityComparer<TElement> comparer)
            {
                if (remaining == 0)
                {
                    // don't build the dictionary at all if nothing should be in it
                    return null;
                }

                const int MaxInitialElementCountsCapacity = 1024;
                var elementCounts = new CountingSet(comparer, capacity: Math.Min(remaining, MaxInitialElementCountsCapacity));
                elementCounts.Increment(elements.Current);
                while (elements.MoveNext())
                {
                    if (--remaining < 0)
                    {
                        // too many elements
                        return null;
                    }
                    elementCounts.Increment(elements.Current);
                }

                if (remaining > 0)
                {
                    // too few elements
                    return null;
                }

                return elementCounts;
            }

            /// <summary>
            /// Key Lookup Reduction optimization: this custom datastructure halves the number of <see cref="IEqualityComparer{T}.GetHashCode(T)"/>
            /// and <see cref="IEqualityComparer{T}.Equals(T, T)"/> operations by building in the increment/decrement operations of a counting dictionary.
            /// This also solves <see cref="Dictionary{TKey, TValue}"/>'s issues with null keys
            /// </summary>
            private sealed class CountingSet
            {
                // picked based on observing unit test performance
                private const double MaxLoad = .62;

                private readonly IEqualityComparer<TElement> comparer;
                private Bucket[] buckets;
                private int populatedBucketCount;
                /// <summary>
                /// When we reach this count, we need to resize
                /// </summary>
                private int nextResizeCount;

                public CountingSet(IEqualityComparer<TElement> comparer, int capacity = 0)
                {
                    this.comparer = comparer;
                    // we pick the initial length by assuming our current table is one short of the desired
                    // capacity and then using our standard logic of picking the next valid table size
                    this.buckets = new Bucket[GetNextTableSize((int)(capacity / MaxLoad) - 1)];
                    this.nextResizeCount = this.CalculateNextResizeCount();
                }

                public bool IsEmpty => this.populatedBucketCount == 0;

                public void Increment(TElement item)
                {
                    if (this.TryFindBucket(item, out var bucketIndex, out var hashCode))
                    {
                        // if a bucket already existed, just update it's count
                        ++this.buckets[bucketIndex].Count;
                    }
                    else
                    {
                        // otherwise, claim a new bucket
                        this.buckets[bucketIndex].HashCode = hashCode;
                        this.buckets[bucketIndex].Value = item;
                        this.buckets[bucketIndex].Count = 1;
                        ++this.populatedBucketCount;

                        // resize the table if we've grown too full
                        if (this.populatedBucketCount == this.nextResizeCount)
                        {
                            var newBuckets = new Bucket[GetNextTableSize(this.buckets.Length)];

                            // rehash
                            for (var i = 0; i < this.buckets.Length; ++i)
                            {
                                var oldBucket = this.buckets[i];
                                if (oldBucket.HashCode != 0)
                                {
                                    var newBucketIndex = oldBucket.HashCode % newBuckets.Length;
                                    while (true)
                                    {
                                        if (newBuckets[newBucketIndex].HashCode == 0)
                                        {
                                            newBuckets[newBucketIndex] = oldBucket;
                                            break;
                                        }

                                        newBucketIndex = (newBucketIndex + 1) % newBuckets.Length;
                                    }
                                }
                            }

                            this.buckets = newBuckets;
                            this.nextResizeCount = this.CalculateNextResizeCount();
                        }
                    }
                }

                public bool TryDecrement(TElement item)
                {
                    if (this.TryFindBucket(item, out var bucketIndex, out _)
                        && this.buckets[bucketIndex].Count > 0)
                    {
                        if (--this.buckets[bucketIndex].Count == 0)
                        {
                            --this.populatedBucketCount;
                        }
                        return true;
                    }

                    return false;
                }

                private bool TryFindBucket(TElement item, out int index, out uint hashCode)
                {
                    // we convert the raw hash code to a uint to get correctly-signed mod operations
                    // and get rid of the zero value so that we can use 0 to mean "unoccupied"
                    var rawHashCode = this.comparer.GetHashCode(item);
                    hashCode = rawHashCode == 0 ? uint.MaxValue : unchecked((uint)rawHashCode);

                    var bestBucketIndex = (int)(hashCode % this.buckets.Length);

                    var bucketIndex = bestBucketIndex;
                    while (true) // guaranteed to terminate because of how we set load factor
                    {
                        var bucket = this.buckets[bucketIndex];
                        if (bucket.HashCode == 0)
                        {
                            // found unoccupied bucket
                            index = bucketIndex;
                            return false;
                        }
                        if (bucket.HashCode == hashCode && this.comparer.Equals(bucket.Value, item))
                        {
                            // found matching bucket
                            index = bucketIndex;
                            return true;
                        }

                        // otherwise march on to the next adjacent bucket
                        bucketIndex = (bucketIndex + 1) % this.buckets.Length;
                    }
                }

                private int CalculateNextResizeCount() => (int)(MaxLoad * this.buckets.Length) + 1;

                private static readonly int[] HashTableSizes = new[]
                {
                    // hash table primes from http://planetmath.org/goodhashtableprimes
                    23, 53, 97, 193, 389, 769, 1543, 3079, 6151, 12289,
                    24593, 49157, 98317, 196613, 393241, 786433, 1572869,
                    3145739, 6291469, 12582917, 25165843, 50331653, 100663319,
                    201326611, 402653189, 805306457, 1610612741,
                    // the first two values are (1) a prime roughly half way between the previous value and int.MaxValue
                    // and (2) the prime closest too, but not above, int.MaxValue. The maximum size is, of course, int.MaxValue
                    1879048201, 2147483629, int.MaxValue
                };

                private static int GetNextTableSize(int currentSize)
                {
                    for (var i = 0; i < HashTableSizes.Length; ++i)
                    {
                        var nextSize = HashTableSizes[i];
                        if (nextSize > currentSize) { return nextSize; }
                    }

                    throw new InvalidOperationException("Hash table cannot expand further");
                }

                [DebuggerDisplay("{Value}, {Count}, {HashCode}")]
                private struct Bucket
                {
                    // note: 0 (default) means the bucket is unoccupied
                    internal uint HashCode;
                    internal TElement Value;
                    internal int Count;
                }
            }

            private static bool TryFastCount(IEnumerable<TElement> source, out int count)
            {
                if (source is IReadOnlyCollection<TElement> readOnlyCollection) 
                {
                    count = readOnlyCollection.Count;
                    return true;
                }

                if (source is ICollection<TElement> collection)
                {
                    count = collection.Count;
                    return true;
                }

                count = -1;
                return false;
            }
        }

        private static int ComputeOrderAgnosticHash<TElement>(IEnumerable<TElement> enumerable, IEqualityComparer<TElement> elementComparer)
        {
            var hash = HashHelper.StarterPrime;
            var count = 0;
            foreach (var element in enumerable)
            {
                // use xor to be order-agnostic
                hash ^= elementComparer.GetHashCode(element);
                unchecked { ++count; }
            }
            return HashHelper.Combine(count, hash);
        }
    }
}
