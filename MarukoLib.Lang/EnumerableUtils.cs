using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarukoLib.Lang
{

    public static class EnumerableUtils
    {

        public static IEnumerable<T> Enumerate<T>(int total, Func<int, T> func)
        {
            for (var i = 0; i < total; i++)
                yield return func(i);
        }

        public static int Count(this IEnumerable enumerable) => Enumerable.Count(enumerable.Cast<object>());

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> enumerable, Func<T, bool> filter) => enumerable.Where(filter);

        public static string Join<T>(this IEnumerable<T> enumerable, string separator, Func<T, object> convertFunc = null)
        {
            if (convertFunc == null)
                convertFunc = t => t;

            var builder = new StringBuilder();
            foreach (var item in enumerable)
            {
                if (builder.Length > 0)
                    builder.Append(separator);
                builder.Append(convertFunc(item));
            }

            return builder.ToString();
        }

        public static IEnumerable<IReadOnlyCollection<T>> MovingWindows<T>(this IEnumerable<T> values, uint windowSize, double overlap)
        {
            if (windowSize == 0) throw new ArgumentException("window size must be positive");
            if (overlap < 0 || overlap >= 1) throw new ArgumentException("overlap must in range of [0, 1)");
            var overlapSize = Math.Min((uint)(windowSize * overlap), windowSize - 1);
            var windowStep = windowSize - overlapSize;
            var samples = new LinkedList<T>();
            uint counter = 0;
            foreach (var value in values)
            {
                samples.AddLast(value);
                counter++;
                if (samples.Count >= windowSize)
                {
                    if (samples.Count > windowSize)
                        samples.RemoveFirst();
                    if (counter >= windowStep)
                    {
                        counter = 0;
                        yield return samples.AsReadonly();
                    }
                }
            }
        }

    }
}
