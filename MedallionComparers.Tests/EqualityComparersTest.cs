using Medallion.Collections;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedallionComparers.Tests
{
    public class EqualityComparersTest
    {
        [Test]
        public void TestCreate()
        {
            Assert.Throws<ArgumentNullException>(() => EqualityComparers.Create<int>(null));

            var comparer = EqualityComparers.Create<string>((a, b) => a.Length == b.Length);
            Assert.IsTrue(comparer.Equals("a", "b"));
            Assert.IsFalse(comparer.Equals(null, string.Empty));
            Assert.IsFalse(comparer.Equals(string.Empty, null));
            Assert.IsFalse(comparer.Equals("aa", "b"));
            Assert.AreEqual(-1, comparer.GetHashCode("abc"));

            comparer = EqualityComparers.Create<string>((a, b) => a.Length == b.Length, s => s.Length);
            Assert.IsTrue(comparer.Equals("a", "b"));
            Assert.IsFalse(comparer.Equals(null, string.Empty));
            Assert.IsFalse(comparer.Equals(string.Empty, null));
            Assert.IsFalse(comparer.Equals("aa", "b"));
            Assert.AreEqual(3, comparer.GetHashCode("abc"));
        }

        [Test]
        public void TestCreateByKey()
        {
            var comparer = EqualityComparers.Create((string s) => s.Length);
            Assert.IsTrue(comparer.Equals("a", "b"));
            Assert.IsFalse(comparer.Equals(null, string.Empty));
            Assert.IsFalse(comparer.Equals(string.Empty, null));
            Assert.IsFalse(comparer.Equals("aa", "b"));
            Assert.AreEqual(3, comparer.GetHashCode("abc"));
        }

        [Test]
        public void TestReferenceComparer()
        {
            var referenceComparer = EqualityComparers.GetReferenceComparer<string>();
            Assert.AreSame(referenceComparer, EqualityComparers.GetReferenceComparer<string>());

            Assert.IsFalse(referenceComparer.Equals(new string('a', 1), new string('a', 1)));
            var text = new string('a', 1);
            Assert.IsTrue(referenceComparer.Equals(text, text));
            Assert.AreEqual(referenceComparer.GetHashCode(text), referenceComparer.GetHashCode(text));
            Assert.IsFalse(referenceComparer.Equals(text, null));
            Assert.IsTrue(referenceComparer.Equals(null, null));

            var obj = new object();
            Assert.AreEqual(obj.GetHashCode(), EqualityComparers.GetReferenceComparer<object>().GetHashCode(obj));
        }

        [Test]
        public void TestEnumerableEquivalenceComparer()
        {
            var a = new[] { 1, 2, 3 };
            var b = new[] { 3, 2, 1 };
            var c = new[] { 1, 2, 4 };

            var comparer = EqualityComparers.GetEnumerableEquivalenceComparer<int>();
            Assert.AreSame(comparer, EqualityComparers.GetEnumerableEquivalenceComparer<int>());
            Assert.AreSame(comparer, EqualityComparers.GetEnumerableEquivalenceComparer(EqualityComparer<int>.Default));
            Assert.IsTrue(comparer.Equals(a, b));
            Assert.IsFalse(comparer.Equals(a, null));
            Assert.IsFalse(comparer.Equals(a, c));
            Assert.AreEqual(comparer.GetHashCode(b), comparer.GetHashCode(a));
            Assert.AreNotEqual(comparer.GetHashCode(a), comparer.GetHashCode(c));

            var stringComparer = EqualityComparers.GetEnumerableEquivalenceComparer(StringComparer.OrdinalIgnoreCase);
            var aa = new[] { "a", "B", "C" };
            var bb = new[] { "B", "A", "c" };
            var cc = new[] { "a", "B", "C", "d" };
            Assert.IsTrue(stringComparer.Equals(aa, bb));
            Assert.IsFalse(stringComparer.Equals(aa, cc));
            Assert.AreEqual(stringComparer.GetHashCode(bb), stringComparer.GetHashCode(aa));
            Assert.AreNotEqual(stringComparer.GetHashCode(aa), stringComparer.GetHashCode(cc));
        }

        [Test]
        public void TestSequenceComparer()
        {
            var a = new[] { 1, 2, 3 };
            var b = new[] { 1, 2, 3 };
            var c = new[] { 1, 3, 2 };

            var comparer = EqualityComparers.GetSequenceComparer<int>();
            Assert.AreSame(comparer, EqualityComparers.GetSequenceComparer<int>());
            Assert.AreSame(comparer, EqualityComparers.GetSequenceComparer(EqualityComparer<int>.Default));

            Assert.IsTrue(comparer.Equals(a, b));
            Assert.IsFalse(comparer.Equals(a, c));
            Assert.IsFalse(comparer.Equals(null, a));
            Assert.IsTrue(comparer.Equals(null, null));
            Assert.AreEqual(((IEqualityComparer)comparer).GetHashCode(null), comparer.GetHashCode(null));
            Assert.AreEqual(comparer.GetHashCode(b), comparer.GetHashCode(a));
            Assert.AreNotEqual(comparer.GetHashCode(a), comparer.GetHashCode(c));

            var stringComparer = EqualityComparers.GetSequenceComparer(StringComparer.OrdinalIgnoreCase);
            var aa = new[] { "a", "B", "C" };
            var bb = new[] { "A", "b", "c" };
            var cc = new[] { "a", "B", "C", "d" };
            Assert.IsTrue(stringComparer.Equals(aa, bb));
            Assert.IsFalse(stringComparer.Equals(aa, cc));
            Assert.AreEqual(stringComparer.GetHashCode(bb), stringComparer.GetHashCode(aa));
            Assert.AreNotEqual(stringComparer.GetHashCode(aa), stringComparer.GetHashCode(cc));
        }

        [Test]
        public void TestSetComparer()
        {
            var a = new[] { 1, 2, 3 };
            var b = new[] { 2, 2, 1, 3, 3 };
            var c = new[] { 2, 2, 1, 3, 4 };

            var comparer = EqualityComparers.GetSetComparer<int>();
            Assert.AreSame(comparer, EqualityComparers.GetSetComparer<int>());
            Assert.AreSame(comparer, EqualityComparers.GetSetComparer(EqualityComparer<int>.Default));

            Assert.IsTrue(comparer.Equals(a, b));
            Assert.IsFalse(comparer.Equals(a, c));
            Assert.IsFalse(comparer.Equals(null, a));
            Assert.IsTrue(comparer.Equals(null, null));
            Assert.AreEqual(((IEqualityComparer)comparer).GetHashCode(null), comparer.GetHashCode(null));
            Assert.AreEqual(comparer.GetHashCode(b), comparer.GetHashCode(a));
            Assert.AreNotEqual(comparer.GetHashCode(a), comparer.GetHashCode(c));

            var stringComparer = EqualityComparers.GetSetComparer(StringComparer.OrdinalIgnoreCase);
            var aa = new[] { "a", "B", "C" };
            var bb = new[] { "A", "b", "c", "a" };
            var cc = new[] { "a", "B", "C", "d" };
            Assert.IsTrue(stringComparer.Equals(aa, bb));
            Assert.IsFalse(stringComparer.Equals(aa, cc));
            Assert.AreEqual(stringComparer.GetHashCode(bb), stringComparer.GetHashCode(aa));
            Assert.AreNotEqual(stringComparer.GetHashCode(aa), stringComparer.GetHashCode(cc));
        }

        [Test]
        public void TestSetComparerWithKnownSetTypes()
        {
            var setComparer = EqualityComparers.GetSetComparer<string>();
            var ignoreCaseSetComparer = EqualityComparers.GetSetComparer(StringComparer.OrdinalIgnoreCase);

            var hashSet = new HashSet<string> { "a", "b", "c" };
            var ignoreCaseHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "B", "C" };
            AssertEquality(false, setComparer, hashSet, ignoreCaseHashSet);
            AssertEquality(true, ignoreCaseSetComparer, hashSet, ignoreCaseHashSet);

            var immutableHashSet = ImmutableHashSet.CreateRange(hashSet);
            var immutableIgnoreCaseHashSet = ImmutableHashSet.CreateRange(ignoreCaseHashSet).WithComparer(StringComparer.OrdinalIgnoreCase);
            AssertEquality(false, setComparer, immutableHashSet, immutableIgnoreCaseHashSet);
            AssertEquality(true, ignoreCaseSetComparer, immutableHashSet, immutableIgnoreCaseHashSet);

            AssertEquality(true, setComparer, hashSet, immutableHashSet);
            AssertEquality(true, setComparer, ignoreCaseHashSet, immutableIgnoreCaseHashSet);
            AssertEquality(true, ignoreCaseSetComparer, hashSet, immutableIgnoreCaseHashSet);
            AssertEquality(true, ignoreCaseSetComparer, immutableHashSet, ignoreCaseHashSet);

            void AssertEquality(bool value, IEqualityComparer<IEnumerable<string>> comparer, IEnumerable<string> a, IEnumerable<string> b)
            {
                Assert.AreEqual(value, comparer.Equals(a, b));
                Assert.AreEqual(value, comparer.GetHashCode(a) == comparer.GetHashCode(b));
            }
        }

        [Test]
        public void TestComparerEquality()
        {
            Func<string, string, bool> sameLength = (a, b) => a.Length == b.Length;
            Func<string, int> getLength = s => s.Length;
            ComparersTest.TestEquality(EqualityComparers.Create(sameLength, getLength), EqualityComparers.Create(sameLength, getLength), EqualityComparers.Create(getLength));

            ComparersTest.TestEquality(EqualityComparers.Create(getLength), EqualityComparers.Create(getLength, EqualityComparer<int>.Default), EqualityComparers.Create((string s) => s[0]));

            ComparersTest.TestEquality(EqualityComparers.GetReferenceComparer<string>(), EqualityComparers.GetReferenceComparer<string>(), EqualityComparer<string>.Default);

            ComparersTest.TestEquality(EqualityComparers.GetEnumerableEquivalenceComparer(EqualityComparer<string>.Default), EqualityComparers.GetEnumerableEquivalenceComparer<string>(), EqualityComparers.GetEnumerableEquivalenceComparer(StringComparer.OrdinalIgnoreCase));

            ComparersTest.TestEquality(EqualityComparers.GetSequenceComparer(StringComparer.Ordinal), EqualityComparers.GetSequenceComparer(StringComparer.Ordinal), EqualityComparers.GetEnumerableEquivalenceComparer(StringComparer.Ordinal));

            ComparersTest.TestEquality(EqualityComparers.GetSetComparer(StringComparer.Ordinal), EqualityComparers.GetSetComparer(StringComparer.Ordinal), EqualityComparers.GetEnumerableEquivalenceComparer(StringComparer.Ordinal));
        }
    }
}
