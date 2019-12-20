using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MarukoLib.Lang
{

    public static class CollectionUtils
    {

        public class ReadonlyCollection<T> : IReadOnlyCollection<T>
        {

            private readonly Func<int> _countFunc;

            public ReadonlyCollection(int count, IEnumerable<T> enumerable)
            {
                Enumerable = enumerable;
                _countFunc = () => count;
            }

            public ReadonlyCollection(ICollection<T> collection)
            {
                Enumerable = collection;
                _countFunc = () => collection.Count;
            }

            public static IEnumerable<T> Unwrap(IEnumerable<T> enumerable) =>
                (enumerable as ReadonlyCollection<T>)?.Enumerable ?? enumerable;

            public int Count => _countFunc();

            public IEnumerable<T> Enumerable { get; }

            public IEnumerator<T> GetEnumerator() => Enumerable.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public static IReadOnlyCollection<T> AsReadonly<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection) return AsReadonly(collection);
            return AsReadonlyCollection(enumerable, enumerable.Count());
        }

        public static IReadOnlyCollection<T> AsReadonly<T>(this ICollection<T> collection)
        {
            if (collection is IReadOnlyCollection<T> @readonly) return @readonly;
            return new ReadonlyCollection<T>(collection);
        }

        public static IReadOnlyCollection<T> AsReadonlyCollection<T>(this IEnumerable<T> enumerable, int count) => new ReadonlyCollection<T>(count, enumerable);

        public static TR Collect<T, TR>(this ICollection<T> collection, TR value) where TR : T
        {
            collection.Add(value);
            return value;
        }

        public static bool IsNullOrEmpty(ICollection collection) => collection == null || collection.Count == 0;

        public static bool IsNullOrEmpty<T>(IReadOnlyCollection<T> collection) => collection == null || collection.Count == 0;

        public static bool IsNullOrEmpty<T>(ICollection<T> collection) => collection == null || collection.Count == 0;

        public static bool IsEmpty(this ICollection collection) => collection.Count == 0;

        public static void AddAll<T, TV>(this ICollection<T> collection, IEnumerable<TV> enumerable) where TV : T
        {
            foreach (var tv in enumerable)
                collection.Add(tv);
        }

        public static void RemoveAll<T, TV>(this ICollection<T> collection, IEnumerable<TV> enumerable) where TV : T
        {
            foreach (var tv in enumerable)
                collection.Remove(tv);
        }

    }
}
