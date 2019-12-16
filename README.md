# MedallionComparers

MedallionComparers is a .NET library that makes it easy to create `IEqualityComparer<T>`s and `IComparer<T>`s for any situation. 

MedallionComparers is available for download as a [NuGet package](https://www.nuget.org/packages/MedallionComparers). [![NuGet Status](http://img.shields.io/nuget/v/MedallionComparers.svg?style=flat)](https://www.nuget.org/packages/MedallionComparers/)

[Release notes](#release-notes)

## Documentation

### EqualityComparers

The `EqualityComparers` class contains utility methods related to `IEqualityComparer<T>`.

##### `EqualityComparer<T> EqualityComparers.Create<T>(Func<T, T, bool> equals, Func<T, int>? hash = null)`

Creates an equality comparer from an equality function and, optionally, a hash function. This makes it easy and concise to create custom comparers on the fly. If no hash function is provided, all non-null values will hash to the same code, which therefore makes such a comparer a poor choice for hash-based datastructures.

##### `EqualityComparer<T> Create<T, TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)`

Creates an equality comparer which compares elements of type `T` by first projecting them to type `TKey`. This makes it easy and concise to create custom comparers on the fly.For example, to compare strings by length you could do:
```C#
var equalityComparer = EqualityComparers.Create((string s) => s.Length);
```

### Comparers

## Release notes
- 1.0.0 Initial release
