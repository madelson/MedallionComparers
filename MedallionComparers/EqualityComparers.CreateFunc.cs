using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    /// <summary>
    /// Provides utilities for creating and working with instances of <see cref="IEqualityComparer{T}"/>
    /// </summary>
    public static partial class EqualityComparers
    {
        /// <summary>
        /// Creates an <see cref="EqualityComparer{T}"/> using the given <paramref name="equals"/> function
        /// for equality and the optional <paramref name="hash"/> function for hashing (if <paramref name="hash"/> is not
        /// provided, all values hash to 0). Note that null values are handled directly by the comparer and will not
        /// be passed to these functions
        /// </summary>
        public static EqualityComparer<T> Create<T>(Func<T, T, bool> equals, Func<T, int>? hash = null) =>
            new FuncEqualityComparer<T>(equals ?? throw new ArgumentNullException(nameof(equals)), hash);

        private sealed class FuncEqualityComparer<T> : EqualityComparer<T>
        {
            private static readonly Func<T, int> DefaultHash = _ => HashHelper.StarterPrime;

            private readonly Func<T, T, bool> equals;
            private readonly Func<T, int> hash;

            public FuncEqualityComparer(Func<T, T, bool> equals, Func<T, int>? hash)
            {
                this.equals = equals;
                this.hash = hash ?? DefaultHash;
            }

            public override bool Equals(T x, T y) =>
                // null checks consistent with Equals(object, object)
                x == null ? y == null : y != null && this.equals(x, y);

            public override int GetHashCode(T obj) =>
                // consistent with GetHashCode(object)
                obj == null ? 0 : this.hash(obj);

            public override bool Equals(object obj) =>
                obj is FuncEqualityComparer<T> that
                    && that.equals.Equals(this.equals)
                    && that.hash.Equals(this.hash);

            public override int GetHashCode() => HashHelper.GetHashCode(this.equals, this.hash);
        }
    }
}
