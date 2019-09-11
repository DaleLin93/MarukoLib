using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MarukoLib.Lang
{

    public static class CollectionUtils
    {

        public class ReadonlyCollection<T> : IReadOnlyCollection<T>
        {

            public ReadonlyCollection(int count, IEnumerable<T> enumerable)
            {
                Enumerable = enumerable;
                Count = count;
            }

            public static IEnumerable<T> Unwrap(IEnumerable<T> enumerable) =>
                (enumerable as ReadonlyCollection<T>)?.Enumerable ?? enumerable;

            public int Count { get; }

            public IEnumerable<T> Enumerable { get; }

            public IEnumerator<T> GetEnumerator() => Enumerable.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        }

        public static ReadonlyCollection<T> AsReadonly<T>(this ICollection<T> collection) => new ReadonlyCollection<T>(collection.Count, collection);

        public static ReadonlyCollection<T> AsReadonlyCollection<T>(this IEnumerable<T> enumerable, int count) => new ReadonlyCollection<T>(count, enumerable);

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

    }
}
