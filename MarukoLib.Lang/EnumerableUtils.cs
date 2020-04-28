using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using MarukoLib.Lang.Collections;

namespace MarukoLib.Lang
{

    public static class EnumerableUtils
    {

        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> enumerable)
        {
            var linkedList = new LinkedList<T>();
            linkedList.AddAll(enumerable);
            return linkedList;
        }

        public static IEnumerable<Type> IsSubclassOf([NotNull] this IEnumerable<Type> enumerable, [NotNull] Type type)
            => enumerable.Where(type.IsAssignableFrom);

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public static IReadOnlyCollection<T> AsReadonlyCollection<T>([NotNull] this IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection) return collection.AsReadonly();
            return AsReadonlyCollection(enumerable, enumerable.Count());
        }

        public static IReadOnlyCollection<T> AsReadonlyCollection<T>([NotNull] this IEnumerable<T> enumerable, int count)
            => new CollectionUtils.ReadonlyCollection<T>(count, enumerable);

        public static IEnumerable<T> Enumerate<T>(int count, [NotNull] Func<int, T> func)
        {
            for (var i = 0; i < count; i++)
                yield return func(i);
        }

        public static int Count([NotNull] this IEnumerable enumerable) => Enumerable.Count(enumerable.Cast<object>());

        public static IEnumerable<T> Not<T>([NotNull] this IEnumerable<T> enumerable, [NotNull] Predicate<T> predicate)
            => enumerable.Where(t => !predicate(t));

        public static IEnumerable<T> NotNull<T>([NotNull] this IEnumerable<T?> enumerable) where T : struct
            => enumerable.Where(t => t.HasValue).Select(t => t.Value);

        public static IEnumerable<T> NotNull<T>([NotNull] this IEnumerable<T> enumerable) where T : class
            => enumerable.Where(Predicates.NotNull);

        public static string Join<T>([NotNull] this IEnumerable<T> enumerable, [NotNull] string separator, [CanBeNull] Func<T, object> convertFunc = null)
        {
            if (convertFunc == null) convertFunc = t => t;
            var builder = new StringBuilder();
            foreach (var item in enumerable)
            {
                if (builder.Length > 0) builder.Append(separator);
                builder.Append(convertFunc(item));
            }
            return builder.ToString();
        }

        public static IEnumerable<IReadOnlyList<T>> MovingWindows<T>([NotNull] this IEnumerable<T> values, uint windowSize, double overlap)
        {
            if (windowSize <= 0) throw new ArgumentException("window size must be positive");
            if (overlap < 0 || overlap >= 1) throw new ArgumentException("overlap must in range of [0, 1)");
            var overlapSize = Math.Min((uint)(windowSize * overlap), windowSize - 1);
            var windowStep = windowSize - overlapSize;
            var samples = new CircularFifoBuffer<T>(windowSize);
            uint counter = 0;
            foreach (var value in values)
            {
                samples.Add(value);
                counter++;
                if (samples.Count >= windowSize && counter >= windowStep)
                {
                    counter = 0;
                    yield return samples;
                }
            }
        }

    }
}
