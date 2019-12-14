using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Collections
{
    internal static class HashHelper
    {
        public static int GetHashCode<T1, T2>(T1 item1, T2 item2) => Combine(EqualityComparer<T1>.Default.GetHashCode(item1), EqualityComparer<T2>.Default.GetHashCode(item2));

        private static int Combine(int hash1, int hash2) => unchecked((3 * hash1) + hash2);
    }
}
