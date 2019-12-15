using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Medallion.Collections
{
    public static partial class EqualityComparers
    {
        /// <summary>
        /// Gets a cached <see cref="EqualityComparer{T}"/> instance which performs all comparisons by reference
        /// (i. e. as if with <see cref="object.ReferenceEquals(object, object)"/>). Uses 
        /// <see cref="RuntimeHelpers.GetHashCode(object)"/> to emulate the native identity-based hash function
        /// </summary>
        public static EqualityComparer<T> GetReferenceComparer<T>() where T : class =>
            ReferenceEqualityComparer<T>.Instance;

        private sealed class ReferenceEqualityComparer<T> : EqualityComparer<T>
            where T : class
        {
            public static readonly EqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

            public override bool Equals(T x, T y) => ReferenceEquals(x, y);

            public override int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
