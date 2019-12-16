using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medallion.Collections
{
    public static partial class EqualityComparers
    {
        /// <summary>
        /// Gets an <see cref="EqualityComparer{T}"/> which compares instances of <see cref="IEnumerable{TElement}"/> as if
        /// with <see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>. The optional 
        /// <paramref name="elementComparer"/> can be used to override the comparison of individual elements
        /// </summary>
        public static EqualityComparer<IEnumerable<TElement>> GetSequenceComparer<TElement>(IEqualityComparer<TElement>? elementComparer = null) =>
            elementComparer == null || elementComparer == EqualityComparer<TElement>.Default
                ? SequenceComparer<TElement>.DefaultInstance
                : new SequenceComparer<TElement>(elementComparer);

        private sealed class SequenceComparer<TElement> : EqualityComparer<IEnumerable<TElement>>
        {
            private static EqualityComparer<IEnumerable<TElement>>? defaultInstance;
            public static EqualityComparer<IEnumerable<TElement>> DefaultInstance => defaultInstance ??= new SequenceComparer<TElement>(EqualityComparer<TElement>.Default);

            private readonly IEqualityComparer<TElement> elementComparer;

            public SequenceComparer(IEqualityComparer<TElement> elementComparer)
            {
                this.elementComparer = elementComparer;
            }

            public override bool Equals(IEnumerable<TElement> x, IEnumerable<TElement> y)
            {
                if (x == y) { return true; }
                if (x == null || y == null) { return false; }

                return x.SequenceEqual(y, this.elementComparer);
            }

            public override int GetHashCode(IEnumerable<TElement> obj)
            {
                if (obj == null) { return 0; }

                var hash = HashHelper.StarterPrime;
                foreach (var element in obj)
                {
                    hash = HashHelper.Combine(hash, this.elementComparer.GetHashCode(element));
                }
                return hash;
            }

            public override bool Equals(object obj) => obj is SequenceComparer<TElement> that && that.elementComparer.Equals(this.elementComparer);

            // including this.GetType() so that we don't hash-collide with the underlying comparer
            public override int GetHashCode() => HashHelper.GetHashCode(this.GetType(), this.elementComparer);
        }
    }
}
