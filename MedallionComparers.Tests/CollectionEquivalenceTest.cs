using Medallion.Collections;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MedallionComparers.Tests
{
    public class CollectionEquivalenceTest
    {
        [Test]
        public void TestEqualSequences()
        {
            Assert.IsTrue(AreEquivalent(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }));
            Assert.IsTrue(AreEquivalent(Enumerable.Range(0, 10000), Enumerable.Range(0, 10000)));
            Assert.IsTrue(AreEquivalent(Sequence("a", "b", "c"), Sequence("A", "B", "C"), StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public void TestRapidExit()
        {
            var comparer = EqualityComparers.Create(
                (int a, int b) => { throw new InvalidOperationException("should never get here"); },
                i => { throw new InvalidOperationException("should never get here"); }
            );

            Assert.IsTrue(AreEquivalent(Sequence<int>(), Sequence<int>(), comparer));
            Assert.IsTrue(AreEquivalent(new int[0], new int[0], comparer));

            Assert.IsFalse(AreEquivalent(new[] { 1, 2, 3 }, new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void TestNullElements()
        {
            Assert.IsTrue(AreEquivalent(Sequence<int?>(1, 2, 3, 4, null), Sequence<int?>(1, 2, 3, 4, null)));
            Assert.IsFalse(AreEquivalent(Sequence<int?>(1, 2, 3, 4, null), Sequence<int?>(null, 2, 3, 1, 5)));
            Assert.IsTrue(AreEquivalent(Sequence<int?>(null, null), Sequence<int?>(null, null)));
            Assert.IsFalse(AreEquivalent(Sequence<int?>(null, null, null), new int?[] { null, null }));
        }

        [Test]
        public void TestOutOfOrder()
        {
            Assert.IsTrue(AreEquivalent(
                Sequence("Apple", "Banana", "Carrot"),
                Sequence("carrot", "banana", "apple"),
                StringComparer.OrdinalIgnoreCase
            ));
        }

        [Test]
        public void TestDuplicates()
        {
            Assert.IsTrue(AreEquivalent(Enumerable.Repeat('a', 1000), Enumerable.Repeat('a', 1000)));
            Assert.IsTrue(AreEquivalent(
                Enumerable.Repeat('a', 1000).Concat(Enumerable.Repeat('b', 1000)),
                Enumerable.Repeat('b', 1000).Concat(Enumerable.Repeat('a', 1000))
            ));

            Assert.IsTrue(AreEquivalent(new[] { 1, 1, 2, 2, 3, 3, }, Sequence(1, 2, 3, 1, 2, 3)));
            Assert.IsFalse(AreEquivalent(new[] { 1, 1, 2, 2, 3, 3, }, Sequence(1, 2, 1, 2, 3, 4)));
        }

        [Test]
        public void TestCustomComparer()
        {
            Assert.IsTrue(AreEquivalent(new[] { "a", "b" }, Sequence("B", "A"), StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public void TestSmartBuildSideProbeSideChoice()
        {
            var longerButThrows = new CountingEnumerableCollection<int>(ThrowsAt(Enumerable.Range(0, 10), index: 9), count: 10);
            var shorter = Enumerable.Range(0, 9).Reverse(); // force us into build/probe mode

            Assert.IsFalse(AreEquivalent(longerButThrows, shorter));
            Assert.IsFalse(AreEquivalent(shorter, longerButThrows));
        }

        [Test]
        public void FuzzTest()
        {
            var random = new System.Random(12345);

            for (var i = 0; i < 10000; ++i)
            {
                var count = random.Next(1000);
                var sequence1 = Enumerable.Range(0, count).Select(_ => random.Next(count)).ToList();
                var sequence2 = sequence1.OrderBy(_ => random.Next()).ToList();
                var equal = random.Next(2) == 1;
                if (!equal)
                {
                    switch (count == 0 ? 4 : random.Next(5))
                    {
                        case 0:
                            sequence2[random.Next(count)]++;
                            break;
                        case 1:
                            var toChange = random.Next(1, count + 1);
                            for (var j = 0; j < toChange; ++j)
                            {
                                sequence2[j]++;
                            }
                            break;
                        case 2:
                            sequence2.RemoveAt(random.Next(count));
                            break;
                        case 3:
                            var toRemove = random.Next(1, count + 1);
                            sequence2 = sequence2.Skip(toRemove).ToList();
                            break;
                        case 4:
                            var toAdd = random.Next(1, count + 1);
                            sequence2.AddRange(Enumerable.Repeat(random.Next(count), toAdd));
                            break;
                        default:
                            throw new InvalidOperationException("should never get here");
                    }
                }

                var toCompare1 = random.Next(2) == 1 ? sequence1 : sequence1.Where(_ => true);
                var toCompare2 = random.Next(2) == 1 ? sequence2 : sequence2.Where(_ => true);

                try
                {
                    Assert.AreEqual(equal, AreEquivalent(toCompare1, toCompare2));
                }
                catch
                {
                    Console.WriteLine($"Case {i} failed");
                    throw;
                }
            }
        }

        #region ---- Comparsion Stuff ----
        [Test]
        public void ComparisonTest()
        {
            var results = new Dictionary<string, ComparisonResult>();

            results.Add("arrays of different lengths", ComparisonProfile(Enumerable.Range(0, 1000).ToArray(), Enumerable.Range(0, 1001).ToArray()));

            results.Add("long array short lazy", ComparisonProfile(Enumerable.Range(0, 1000).Reverse().ToArray(), Enumerable.Range(0, 500)));

            results.Add("short array long lazy", ComparisonProfile(Enumerable.Range(0, 1000).Reverse(), Enumerable.Range(0, 500).ToArray()));

            results.Add("sequence equal", ComparisonProfile(Enumerable.Range(0, 1000), Enumerable.Range(0, 1000)));

            results.Add("mostly sequence equal", ComparisonProfile(Enumerable.Range(0, 1000).Append(int.MaxValue), Enumerable.Range(0, 1000).Append(int.MaxValue)));

            results.Add("equal out of order", ComparisonProfile(Enumerable.Range(0, 1000), Enumerable.Range(0, 1000).OrderByDescending(i => i).ToArray()));

            var strings = Enumerable.Range(0, 1000).Select(i => (i + (long)int.MaxValue).ToString("0000000000000000000"))
                .ToArray();
            results.Add("strings equal out of order", ComparisonProfile(strings, strings.Reverse()));

            foreach (var kvp in results)
            {
                Console.WriteLine($"---- {kvp.Key} ----");

                var cer = kvp.Value.CollectionEqualsResult;
                var dmr = kvp.Value.DictionaryMethodResult;
                Func<double, double, string> perc = (n, d) => (n / d).ToString("0.0%");
                Console.WriteLine($"{perc(cer.Duration.Ticks, dmr.Duration.Ticks)}, {perc(cer.EnumerateCount, dmr.EnumerateCount)} {perc(cer.EqualsCount, dmr.EqualsCount)}, {perc(cer.HashCount, dmr.HashCount)}");
                Console.WriteLine($"CollectionEquals: {kvp.Value.CollectionEqualsResult}");
                Console.WriteLine($"Dictionary: {kvp.Value.DictionaryMethodResult}");
                
                kvp.Value.CollectionEqualsResult.AssertBetterThan(kvp.Value.DictionaryMethodResult);
            }
        }

        private static ComparisonResult ComparisonProfile<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            return new ComparisonResult
            {
                CollectionEqualsResult = Profile(a, b, (a, b, c) => EqualityComparers.GetCollectionComparer(c).Equals(a, b)),
                DictionaryMethodResult = Profile(a, b, DictionaryBasedEquals),
            };
        }

        private static ProfilingResult Profile<T>(
            IEnumerable<T> a,
            IEnumerable<T> b,
            Func<IEnumerable<T>, IEnumerable<T>, IEqualityComparer<T>, bool> equals)
        {
            // capture base stats
            var wrappedA = a is IReadOnlyCollection<T> ? new CountingEnumerableCollection<T>((IReadOnlyCollection<T>)a) : new CountingEnumerable<T>(a);
            var wrappedB = b is IReadOnlyCollection<T> ? new CountingEnumerableCollection<T>((IReadOnlyCollection<T>)b) : new CountingEnumerable<T>(b);
            var comparer = new CountingEqualityComparer<T>();
            equals(wrappedA, wrappedB, comparer);

            // capture timing stats
            const int Trials = 1000;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var originalThreadPriority = Thread.CurrentThread.Priority;
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < Trials; ++i)
                {
                    equals(a, b, EqualityComparer<T>.Default);
                }

                return new ProfilingResult
                {
                    Duration = stopwatch.Elapsed,
                    EnumerateCount = wrappedA.EnumerateCount + wrappedB.EnumerateCount,
                    EqualsCount = comparer.EqualsCount,
                    HashCount = comparer.HashCount,
                };
            }
            finally
            {
                Thread.CurrentThread.Priority = originalThreadPriority;
            }
        }

        private class ComparisonResult
        {
            public ProfilingResult CollectionEqualsResult { get; set; }
            public ProfilingResult DictionaryMethodResult { get; set; }
        }

        private class ProfilingResult
        {
            public TimeSpan Duration { get; set; }
            public long EnumerateCount { get; set; }
            public long EqualsCount { get; set; }
            public long HashCount { get; set; }

            public override string ToString() => $"Duration={this.Duration}, Enumerate={this.EnumerateCount}, Equals={this.EqualsCount}, Hash={this.HashCount}";

            public void AssertBetterThan(ProfilingResult that)
            {
#if DEBUG
                var durationScore = 0; // not accurrate in debug mode
#else
                var durationScore = (this.Duration - that.Duration).Duration() < TimeSpan.FromMilliseconds(this.Duration.TotalMilliseconds / 200)
                    ? 0
                    : this.Duration.CompareTo(that.Duration);
#endif

                var enumerateScore = this.EnumerateCount.CompareTo(that.EnumerateCount);
                // allow equals to vary by 1 because of the sequence equal optimization
                var equalsScore = Math.Abs(this.EqualsCount - that.EqualsCount) > 1 ? this.EqualsCount.CompareTo(that.EqualsCount) : 0;
                var hashScore = this.HashCount.CompareTo(that.HashCount);

                var scores = new[] { durationScore, enumerateScore, equalsScore, hashScore };
                Assert.True(scores.All(i => i <= 0), "Scores: " + string.Join(", ", scores));
                Assert.True(scores.Any(i => i < 0), "Scores: " + string.Join(", ", scores));
            }
        }

        private static bool SortBasedEquals<T>(IEnumerable<T> a, IEnumerable<T> b, IEqualityComparer<T> comparer)
        {
            var order = Comparers.Create((T item) => comparer.GetHashCode(item));
            return a.OrderBy(x => x, order).SequenceEqual(b.OrderBy(x => x, order), comparer);
        }

        private static bool DictionaryBasedEquals<T>(IEnumerable<T> a, IEnumerable<T> b, IEqualityComparer<T> comparer)
        {
            var dictionary = new Dictionary<T, int>(comparer);
            foreach (var item in a)
            {
                int existingCount;
                if (dictionary.TryGetValue(item, out existingCount))
                {
                    dictionary[item] = existingCount + 1;
                }
                else
                {
                    dictionary.Add(item, 1);
                }
            }

            foreach (var item in b)
            {
                int count;
                if (!dictionary.TryGetValue(item, out count))
                {
                    return false;
                }
                if (count == 1)
                {
                    dictionary.Remove(item);
                }
                else
                {
                    dictionary[item] = count - 1;
                }
            }

            return dictionary.Count == 0;
        }

        private sealed class CountingEqualityComparer<T> : IEqualityComparer<T>
        {
            public long EqualsCount { get; private set; }
            public long HashCount { get; private set; }

            bool IEqualityComparer<T>.Equals(T x, T y)
            {
                this.EqualsCount++;
                return EqualityComparer<T>.Default.Equals(x, y);
            }

            int IEqualityComparer<T>.GetHashCode(T obj)
            {
                this.HashCount++;
                return EqualityComparer<T>.Default.GetHashCode(obj);
            }
        }

        private class CountingEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> enumerable;

            public CountingEnumerable(IEnumerable<T> enumerable)
            {
                this.enumerable = enumerable;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public long EnumerateCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in this.enumerable)
                {
                    this.EnumerateCount++;
                    yield return item;
                }
            }
        }

        private class CountingEnumerableCollection<T> : CountingEnumerable<T>, IReadOnlyCollection<T>
        {
            public int Count { get; private set; }

            public CountingEnumerableCollection(IEnumerable<T> sequence, int count)
                : base(sequence)
            {
                this.Count = count;
            }

            public CountingEnumerableCollection(IReadOnlyCollection<T> collection)
               : this(collection, collection.Count)
            {
            }
        }
#endregion

        private static IEnumerable<T> Sequence<T>(params T[] items) { return items.Where(i => true); }

        private static IEnumerable<T> ThrowsAt<T>(IEnumerable<T> items, int index)
        {
            var i = 0;
            foreach (var item in items)
            {
                if (i == index)
                {
                    Assert.False(true, "ThrowsAt failure!");
                }
                yield return item;
                ++i;
            }
        }

        private static bool AreEquivalent<T>(IEnumerable<T> a, IEnumerable<T> b, IEqualityComparer<T> comparer = null) => 
            EqualityComparers.GetCollectionComparer<T>(comparer).Equals(a, b);
    }
}
