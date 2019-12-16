# MedallionComparers

MedallionComparers is a .NET library that makes it easy to create `IEqualityComparer<T>`s and `IComparer<T>`s for any situation, including comparing collections.

MedallionComparers is available for download as a [NuGet package](https://www.nuget.org/packages/MedallionComparers). [![NuGet Status](http://img.shields.io/nuget/v/MedallionComparers.svg?style=flat)](https://www.nuget.org/packages/MedallionComparers/)

[Release notes](#release-notes)

## Documentation

### EqualityComparers

The `EqualityComparers` class contains utility methods related to `IEqualityComparer<T>`.

##### `EqualityComparer<T> EqualityComparers.Create<T>(Func<T, T, bool> equals, Func<T, int>? hash = null)`

Creates an equality comparer from an equality function and, optionally, a hash function. This makes it easy and concise to create custom comparers on the fly. If no hash function is provided, all non-null values will hash to the same code, which therefore makes such a comparer a poor choice for hash-based datastructures.

##### `EqualityComparer<T> Create<T, TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)`

Creates an equality comparer which compares elements of type `T` by first projecting them to type `TKey`. This makes it easy and concise to create custom comparers on the fly. For example, to compare strings by length you could do:
```C#
var equalityComparer = EqualityComparers.Create((string s) => s.Length);
```

##### `EqualityComparer<T> GetReferenceComparer<T>() where T : class`

Creates an equality comparer which always uses object identity (`ReferenceEquals`) for comparison *and* hashing, regardless of whether type `T` overrides `Equals` and `GetHashCode`.

##### `IEnumerable<T>` comparers

The library contains several factory methods for comparing `IEnumerable<T>` instances in different ways:
```
EqualityComparer<IEnumerable<TElement>> GetSequenceComparer<TElement>(IEqualityComparer<TElement>? elementComparer = null)
EqualityComparer<IEnumerable<TElement>> GetCollectionComparer<TElement>(IEqualityComparer<TElement>? elementComparer = null)
EqualityComparer<IEnumerable<TElement>> GetSetComparer<TElement>(IEqualityComparer<TElement>? elementComparer = null)
```

When hashing or determining equality, `GetSequenceComparer` retains duplicates and cares about order, `GetCollectionComparer` retains duplicates and disregards order, and `GetSetComparer` disregards both duplicates and order.

In each case, an optional `IEqualityComparer<TElement>` can be specified to control the comparison of individual elements in the enumerables.

Each comparer is designed for speed, for example each will iterate the enumerable(s) only once during an equality or hashing operation. `GetSequenceComparer` leverages `Enumerable.SequenceEqual` under the hood, which contains various common-case optimizations. `GetCollectionComparer` uses the highly-optimized approach described [here](https://github.com/steaks/codeducky/blob/master/blogs/CollectionEquals.md). `GetSetComparer` is optimized for when at least one of the enumerables is already a set (`HashSet<T>` or `ImmutableHashSet<T>`) whose element comparer matches `elementComparer`.

### Comparers

The `Comparers` class contains utility methods related to `IComparer<T>`.

##### Comparer<T> Create<T, TKey>(Func<T, TKey> keySelector, IComparer<TKey>? keyComparer = null)

Creates a comparer which compares elements of type `T` by first projecting them to type `TKey`, similar to what happens with `Enumerable.OrderBy`. This makes it easy and concise to create custom comparers on the fly.

##### Reverse

The extension method `IComparer<T> Reverse<T>(this IComparer<T> comparer)` returns a comparer whose ordering is the opposite of the given comparer. The static utility `IComparer<T> Reverse<T>()` returns a cached comparer instance which is the equivalent of calling `Comparer<T>.Default.Reverse()`.

##### IComparer<T> ThenBy<T>(this IComparer<T> first, IComparer<T> second)

An extension method which, combines two comparers by creating a new comparer that uses the first comparer and falls back to the second comparer to break ties. For example, to compare points by X and then by Y you could do:
```C#
var xThenYComparer = Comparer.Create((Point p) => p.X)
	.ThenBy(Comparer.Create((Point p) => p.X));
```

##### Comparer<IEnumerable<T>> GetSequenceComparer<T>(IComparer<T>? elementComparer = null)

Gets a comparer which sorts sequences *lexicographically*, with an optional element comparer to determine how individual elements are compared.

## Release notes
- 1.0.0 Initial release
