using Medallion.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedallionComparers.Tests
{
    public class ComparersTest
    {
        [Test]
        public void TestCreate()
        {
            var allEqual = Comparers.Create((int? i) => 0);
            Assert.AreEqual(0, allEqual.Compare(1, 5));
            Assert.AreEqual(-1, Math.Sign(allEqual.Compare(null, 5)));
            Assert.AreEqual(1, Math.Sign(allEqual.Compare(5, null)));

            var evenNumbersFirst = Comparers.Create((int i) => i % 2);
            CollectionAssert.AreEqual(
                new[] { 0, 2, 4, 6, 8, 1, 3, 5, 7, 9 },
                Enumerable.Range(0, 10).OrderBy(i => i, evenNumbersFirst)
            );
        }

        [Test]
        public void TestReverse()
        {
            CollectionAssert.AreEqual(
                new[] { 4, 3, 2, 1, 0 },
                Enumerable.Range(0, 5).OrderBy(i => i, Comparers.Reverse<int>())
            );

            Assert.AreSame(Comparers.Reverse<int>(), Comparers.Reverse<int>());
            Assert.AreSame(Comparers.Reverse<int>(), Comparer<int>.Default.Reverse());

            var backwardsStringComparer = Comparer<string>.Create((s1, s2) => string.CompareOrdinal(string.Join(string.Empty, s1.Reverse()), string.Join(string.Empty, s2.Reverse())));
            var reverseBackwardsStringComparer = backwardsStringComparer.Reverse();

            CollectionAssert.AreEqual(
                new[] { "abc", "bac", "cab" },
                new[] { "abc", "cab", "bac" }.OrderBy(s => s, reverseBackwardsStringComparer)
            );

            Assert.Throws<ArgumentNullException>(() => reverseBackwardsStringComparer.Compare("a", null));
            Assert.AreEqual(-1, Math.Sign(Comparer<int?>.Default.Reverse().Compare(1, null)));
        }

        [Test]
        public void TestThenBy()
        {
            var sequence = new (int X, int Y)[] { (1, 2), (1, 1), (-1, 2), (-1, 1) };
            CollectionAssert.AreEqual(
                sequence.OrderByDescending(t => t.X).ThenBy(t => t.Y),
                sequence.OrderBy(t => t, Comparers.Create(((int X, int Y) p) => p.X).Reverse().ThenBy(Comparers.Create(((int X, int Y) p) => p.Y)))
            );
        }

        [Test]
        public void TestSequenceComparer()
        {
            Assert.AreSame(Comparers.GetSequenceComparer<int>(), Comparers.GetSequenceComparer<int>());
            Assert.AreSame(Comparers.GetSequenceComparer<int>(), Comparers.GetSequenceComparer(Comparer<int>.Default));

            var sequence = new[] { new[] { 1, 2, 3 }, new[] { 1, 1, 5 }, new[] { 1, 2, 1 } };
            CollectionAssert.AreEqual(
                new[] { "1,1,5", "1,2,1", "1,2,3" },
                sequence.OrderBy(i => i, Comparers.GetSequenceComparer<int>())
                    .Select(a => string.Join(",", a))
            );

            CollectionAssert.AreEqual(
                new[] { "1,2,3", "1,2,1", "1,1,5" },
                sequence.OrderBy(i => i, Comparers.GetSequenceComparer(Comparer<int>.Default.Reverse()))
                    .Select(a => string.Join(",", a))
            );
            
            Assert.AreEqual(-1, Math.Sign(Comparers.GetSequenceComparer<int>().Compare(Array.Empty<int>(), new[] { 1 })));
        }

        [Test]
        public void TestComparerEquality()
        {
            Func<int, int> f = i => -i;
            TestEquality(Comparers.Create(f), Comparers.Create(f), Comparers.Create((int i) => i.ToString()));
            Assert.AreNotEqual(Comparer<int>.Default.GetHashCode(), Comparers.Create(f).GetHashCode());

            TestEquality(Comparers.Reverse<int>(), Comparers.Reverse<int>(), Comparers.Create((int i) => i.ToString()).Reverse());
            Assert.AreNotEqual(Comparer<int>.Default.GetHashCode(), Comparers.Reverse<int>().GetHashCode());

            var first = Comparers.Create((string s) => s.Length);
            var second = Comparers.Create((string s) => s[0]);
            TestEquality(first.ThenBy(second), first.ThenBy(second), second.ThenBy(first));
            Assert.AreNotEqual(Comparer<string>.Default.GetHashCode(), first.GetHashCode());

            TestEquality(Comparers.GetSequenceComparer<string>(), Comparers.GetSequenceComparer(Comparer<string>.Default), Comparers.GetSequenceComparer(StringComparer.OrdinalIgnoreCase));
            Assert.AreNotEqual(Comparer<string>.Default.GetHashCode(), Comparers.GetSequenceComparer<string>().GetHashCode());
        }

        public static void TestEquality(object obj, object equal, object notEqual)
        {
            Assert.IsTrue(Equals(obj, equal));
            Assert.IsFalse(Equals(obj, notEqual));
            Assert.AreEqual(equal.GetHashCode(), obj.GetHashCode());
            Assert.AreNotEqual(notEqual.GetHashCode(), obj.GetHashCode());
        }
    }
}
