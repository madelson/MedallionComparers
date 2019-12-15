using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Medallion.Collections
{
    public static partial class EqualityComparers
    {
        /// <summary>
        /// Gets an <see cref="EqualityComparer{T}"/> which compares instances of <see cref="IEnumerable{TElement}"/> as if
        /// by converting one to a <see cref="HashSet{T}"/> and using <see cref="HashSet{T}.SetEquals(IEnumerable{T})"/>.
        /// The optional <paramref name="elementComparer"/> can be used to override the comparison of individual elements.
        /// </summary>
        public static EqualityComparer<IEnumerable<TElement>> GetSetComparer<TElement>(IEqualityComparer<TElement>? elementComparer = null) =>
            elementComparer == null || elementComparer == EqualityComparer<TElement>.Default
                ? SetEqualityComparer<TElement>.DefaultInstance
                : new SetEqualityComparer<TElement>(elementComparer);

        private sealed class SetEqualityComparer<TElement> : EqualityComparer<IEnumerable<TElement>>
        {
            private static SetEqualityComparer<TElement>? _cachedDefaultInstance;
            public static SetEqualityComparer<TElement> DefaultInstance => _cachedDefaultInstance ??= new SetEqualityComparer<TElement>(EqualityComparer<TElement>.Default);

            private static IImmutableHashSetComparerRetriever? _cachedImmutableHashSetComparerRetriever;

            private readonly IEqualityComparer<TElement> _elementComparer;

            public SetEqualityComparer(IEqualityComparer<TElement> elementComparer)
            {
                this._elementComparer = elementComparer;
            }

            public override bool Equals(IEnumerable<TElement> x, IEnumerable<TElement> y)
            {
                if (x == y) { return true; }
                if (x == null || y == null) { return false; }

                return this.AsSetWithMatchingElementComparer(x) is { } xSet ? xSet.SetEquals(y)
                    : this.AsSetWithMatchingElementComparer(y) is { } ySet ? ySet.SetEquals(x)
                    : new HashSet<TElement>(x, this._elementComparer).SetEquals(y);
            }

            public override int GetHashCode(IEnumerable<TElement> obj) => 
                obj == null
                    ? 0
                    : ComputeOrderAgnosticHash(
                        this.AsSetWithMatchingElementComparer(obj)
                        ?? new HashSet<TElement>(obj, this._elementComparer),
                        this._elementComparer
                    );

            public override bool Equals(object obj) => obj is SetEqualityComparer<TElement> that && that._elementComparer.Equals(this._elementComparer);

            // including this.GetType() so that we don't hash-collide with the underlying comparer
            public override int GetHashCode() => HashHelper.GetHashCode(this.GetType(), this._elementComparer);

            private ISet<TElement>? AsSetWithMatchingElementComparer(IEnumerable<TElement> enumerable)
            {
                if (enumerable is HashSet<TElement> hashSet)
                {
                    if (this._elementComparer.Equals(hashSet.Comparer))
                    {
                        return hashSet;
                    }
                }
                else if (enumerable is ISet<TElement> possibleImmutableHashSet)
                {
                    var setType = possibleImmutableHashSet.GetType();
                    if (setType.Name == "ImmutableHashSet`1" 
                        && setType.Namespace == "System.Collections.Immutable"
                        && this._elementComparer.Equals((_cachedImmutableHashSetComparerRetriever ??= CreateImmutableHashSetComparerRetriever(setType)).GetComparer(possibleImmutableHashSet)))
                    {
                        return possibleImmutableHashSet;
                    }
                }

                return null;
            }

            private static IImmutableHashSetComparerRetriever CreateImmutableHashSetComparerRetriever(Type setType) =>
                (IImmutableHashSetComparerRetriever)Activator.CreateInstance(
                    typeof(ImmutableHashSetComparerRetriever<>)
                        .MakeGenericType(typeof(TElement), setType)
                );

            private interface IImmutableHashSetComparerRetriever
            {
                public IEqualityComparer<TElement> GetComparer(ISet<TElement> immutableHashSet);
            }

            private class ImmutableHashSetComparerRetriever<TImmutableHashSet> : IImmutableHashSetComparerRetriever
            {
                private readonly Func<TImmutableHashSet, IEqualityComparer<TElement>> _getKeyComparer;

                public ImmutableHashSetComparerRetriever()
                {
                    var keyComparerProperty = typeof(TImmutableHashSet).GetProperty("KeyComparer", BindingFlags.Public | BindingFlags.Instance);
                    this._getKeyComparer = 
                        (Func<TImmutableHashSet, IEqualityComparer<TElement>>)Delegate.CreateDelegate(typeof(Func<TImmutableHashSet, IEqualityComparer<TElement>>), keyComparerProperty.GetMethod);
                }

                IEqualityComparer<TElement> SetEqualityComparer<TElement>.IImmutableHashSetComparerRetriever.GetComparer(ISet<TElement> immutableHashSet) =>
                    this._getKeyComparer((TImmutableHashSet)immutableHashSet);
            }
        }
    }
}
