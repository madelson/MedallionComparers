using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    public static partial class Comparers
    {
        /// <summary>
        /// Gets an <see cref="IComparer{T}"/> which represents the reverse of
        /// the order implied by <see cref="Comparer{T}.Default"/>
        /// </summary>
        public static IComparer<T> Reverse<T>() => ReverseComparer<T>.Default;

        /// <summary>
        /// Gets an <see cref="IComparer{T}"/> which represents the reverse of
        /// the order implied by the given <paramref name="comparer"/>
        /// </summary>
        public static IComparer<T> Reverse<T>(this IComparer<T> comparer)
        {
            return comparer == Comparer<T>.Default
                ? Reverse<T>()
                : new ReverseComparer<T>(comparer ?? throw new ArgumentNullException(nameof(comparer)));
        }

        // we don't want Comparer<T> here because that doesn't let us override
        // the comparison of nulls in the Compare(object, object) method
        private sealed class ReverseComparer<T> : IComparer<T>, IComparer
        {
            private static ReverseComparer<T>? defaultInstance;
            public static ReverseComparer<T> Default => defaultInstance ??= new ReverseComparer<T>(Comparer<T>.Default);

            private readonly IComparer<T> comparer;

            public ReverseComparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public int Compare(T x, T y) => this.comparer.Compare(y, x);

            int IComparer.Compare(object x, object y) => this.Compare((T) x, (T) y);

            public override bool Equals(object obj) => obj is ReverseComparer<T> that && that.comparer.Equals(this.comparer);

            // including this.GetType() so that we don't hash-collide with the underlying comparer
            public override int GetHashCode() => HashHelper.GetHashCode(this.GetType(), this.comparer);
        }
    }
}
