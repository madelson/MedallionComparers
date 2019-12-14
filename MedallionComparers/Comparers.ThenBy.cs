using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    public static partial class Comparers
    {
        /// <summary>
        /// Gets a <see cref="Comparer{T}"/> which compares using <paramref name="first"/>
        /// and breaks ties with <paramref name="second"/>
        /// </summary>
        public static Comparer<T> ThenBy<T>(this IComparer<T> first, IComparer<T> second) => 
            new ThenByComparer<T>(first ?? throw new ArgumentNullException(nameof(first)), second ?? throw new ArgumentNullException(nameof(second)));

        private sealed class ThenByComparer<T> : Comparer<T>
        {
            private readonly IComparer<T> first, second;

            public ThenByComparer(IComparer<T> first, IComparer<T> second)
            {
                this.first = first;
                this.second = second;
            }

            public override int Compare(T x, T y)
            {
                var firstComparison = this.first.Compare(x, y);
                return firstComparison != 0 ? firstComparison : this.second.Compare(x, y);
            }

            public override bool Equals(object obj) => 
                obj is ThenByComparer<T> that
                    && that.first.Equals(this.first)
                    && that.second.Equals(this.second);

            public override int GetHashCode() => HashHelper.GetHashCode(this.first, this.second);
        }
    }
}
