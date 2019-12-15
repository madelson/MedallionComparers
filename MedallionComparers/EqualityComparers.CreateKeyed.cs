using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    public static partial class EqualityComparers
    {
        /// <summary>
        /// Creates an <see cref="EqualityComparer{T}"/> which compares elements of type <typeparamref name="T"/> by projecting
        /// them to an instance of type <typeparamref name="TKey"/> using the provided <paramref name="keySelector"/> and comparing/hashing
        /// these keys. The optional <paramref name="keyComparer"/> argument can be used to specify how the keys are compared. Note that null
        /// values are handled directly by the comparer and will not be passed to <paramref name="keySelector"/>
        /// </summary>
        public static EqualityComparer<T> Create<T, TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null) =>
            new KeyEqualityComparer<T, TKey>(keySelector ?? throw new ArgumentNullException(nameof(keySelector)), keyComparer ?? EqualityComparer<TKey>.Default);

        private sealed class KeyEqualityComparer<T, TKey> : EqualityComparer<T>
        {
            private readonly Func<T, TKey> keySelector;
            private readonly IEqualityComparer<TKey> keyComparer;

            public KeyEqualityComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
            {
                this.keySelector = keySelector;
                this.keyComparer = keyComparer;
            }

            public override bool Equals(T x, T y)
            {
                if (x == null) { return y == null; }
                if (y == null) { return false; }

                return this.keyComparer.Equals(this.keySelector(x), this.keySelector(y));
            }

            public override int GetHashCode(T obj) => obj == null ? 0 : this.keyComparer.GetHashCode(this.keySelector(obj));

            public override bool Equals(object obj) =>
                obj is KeyEqualityComparer<T, TKey> that
                    && that.keySelector.Equals(this.keySelector)
                    && that.keyComparer.Equals(this.keyComparer);

            public override int GetHashCode() => HashHelper.GetHashCode(this.keySelector, this.keyComparer);
        }
    }
}
