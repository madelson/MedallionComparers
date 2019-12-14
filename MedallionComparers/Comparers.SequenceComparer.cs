using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    public static partial class Comparers
    {
        /// <summary>
        /// Gets a <see cref="Comparer{T}"/> which sorts sequences lexographically. The optional
        /// <paramref name="elementComparer"/> can be used to override comparisons of individual elements
        /// </summary>
        public static Comparer<IEnumerable<T>> GetSequenceComparer<T>(IComparer<T>? elementComparer = null)
        {
            return elementComparer == null || elementComparer == Comparer<T>.Default
                ? SequenceComparer<T>.DefaultInstance
                : new SequenceComparer<T>(elementComparer);
        }

        private sealed class SequenceComparer<T> : Comparer<IEnumerable<T>>
        {
            private static Comparer<IEnumerable<T>>? defaultInstance;
            public static Comparer<IEnumerable<T>> DefaultInstance => defaultInstance ??= new SequenceComparer<T>(Comparer<T>.Default);

            private readonly IComparer<T> elementComparer;

            public SequenceComparer(IComparer<T> elementComparer)
            {
                this.elementComparer = elementComparer;
            }

            public override int Compare(IEnumerable<T> x, IEnumerable<T> y)
            {
                if (x == y) { return 0; }
                if (x == null) { return -1; }
                if (y == null) { return 1; }

                using var xEnumerator = x.GetEnumerator();
                using var yEnumerator = y.GetEnumerator();
                while (true)
                {
                    var xHasMore = xEnumerator.MoveNext();
                    var yHasMore = yEnumerator.MoveNext();

                    if (!xHasMore)
                    {
                        return yHasMore ? -1 : 0;
                    }
                    if (!yHasMore)
                    {
                        return 1;
                    }

                    var cmp = this.elementComparer.Compare(xEnumerator.Current, yEnumerator.Current);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }
            }

            public override bool Equals(object obj) => obj is SequenceComparer<T> that && that.elementComparer.Equals(this.elementComparer);

            // including this.GetType() so that we don't hash-collide with the underlying comparer
            public override int GetHashCode() => HashHelper.GetHashCode(this.GetType(), this.elementComparer);
        }
    }
}
