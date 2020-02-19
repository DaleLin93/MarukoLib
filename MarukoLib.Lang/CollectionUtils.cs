using System;
using System.Collections;
using System.Collections.Generic;

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

        public static IReadOnlyCollection<T> AsReadonly<T>(this ICollection<T> collection)
        {
            if (collection is IReadOnlyCollection<T> @readonly) return @readonly;
            return new ReadonlyCollection<T>(collection);
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
