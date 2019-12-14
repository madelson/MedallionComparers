using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    public static partial class Comparers
    {
        /// <summary>
        /// Creates a <see cref="Comparer{T}"/> which compares values of type <typeparamref name="T"/> by
        /// projecting them to type <typeparamref name="TKey"/> using the given <paramref name="keySelector"/>.
        /// The optional <paramref name="keyComparer"/> determines how keys are compared
        /// </summary>
        public static Comparer<T> Create<T, TKey>(Func<T, TKey> keySelector, IComparer<TKey>? keyComparer = null)
        {
            return new KeyComparer<T, TKey>(
                keySelector ?? throw new ArgumentNullException(nameof(keySelector)), 
                keyComparer ?? Comparer<TKey>.Default
            );
        }

        private sealed class KeyComparer<T, TKey> : Comparer<T>
        {
            private readonly Func<T, TKey> keySelector;
            private readonly IComparer<TKey> keyComparer;

            public KeyComparer(Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
            {
                this.keySelector = keySelector;
                this.keyComparer = keyComparer;
            }

            public override int Compare(T x, T y)
            {
                // from Comparer<T>.Compare(object, object)
                if (x == null)
                {
                    return y == null ? 0 : -1;
                }
                if (y == null)
                {
                    return 1;
                }

                return this.keyComparer.Compare(this.keySelector(x), this.keySelector(y));
            }

            public override bool Equals(object obj) => 
                obj is KeyComparer<T, TKey> that
                    && that.keySelector.Equals(this.keySelector)
                    && that.keyComparer.Equals(this.keyComparer);

            public override int GetHashCode() => HashHelper.GetHashCode(this.keySelector, this.keyComparer);
        }
    }
}
