using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MarukoLib.Lang
{

    public static class EmptyArray<T>
    {

        public static readonly T[] Instance = new T[0];

    }

    public static class ArrayUtils
    {

        private sealed class IntRange : IReadOnlyCollection<int>
        {

            private sealed class Enumerator : IEnumerator<int>
            {

                private readonly IntRange _intRange;

                private int _currentIndex = -1;

                public Enumerator(IntRange intRange) => _intRange = intRange;

                public int Current
                {
                    get
                    {
                        if (_currentIndex < 0 || _currentIndex >= _intRange._totalCount)
                            return default;
                        return _intRange._startInclusive + _currentIndex * _intRange._increment;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext() => ++_currentIndex < _intRange._totalCount;

                public void Reset() => _currentIndex = -1;

                public void Dispose() { }

            }

            private readonly int _startInclusive, _increment, _totalCount;

            public IntRange(int start, bool includeStart, int end, bool includeEnd)
            {
                _increment = (end > start) ? +1 : -1;
                _startInclusive = start + (includeStart ? 0 : _increment);
                _totalCount = System.Math.Abs(end + (includeEnd ? _increment : 0) - _startInclusive);
            }

            public int Count => _totalCount;

            public IEnumerator<int> GetEnumerator() => new Enumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        }

        private sealed class DoubleRange : IReadOnlyCollection<double>
        {

            private sealed class Enumerator : IEnumerator<double>
            {

                private readonly DoubleRange _range;

                private int _currentIndex = -1;

                public Enumerator(DoubleRange intRange) => _range = intRange;

                public double Current
                {
                    get
                    {
                        if (_currentIndex < 0 || _currentIndex >= _range.Count)
                            return default;
                        return (double)(_range._startInclusive + _currentIndex * _range._increment);
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext() => ++_currentIndex < _range.Count;

                public void Reset() => _currentIndex = -1;

                public void Dispose() { }

            }

            private readonly decimal _startInclusive, _increment;

            [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
            public DoubleRange(double start, bool includeStart, double end, bool includeEnd, double step)
            {
                _increment = System.Math.Abs((decimal)step) * ((end > start) ? +1 : -1);
                if(_increment==0) throw new ArgumentException("'step' cannot be null");
                _startInclusive =  (decimal)start + (includeStart ? 0M : _increment);

                var endDecimal = (decimal)end;
                var count = (int) System.Math.Floor(System.Math.Abs(endDecimal - _startInclusive) / System.Math.Abs(_increment)) + 1;
                if ((double) (_startInclusive + count * _increment) == end && includeEnd)
                    count++;
                Count = count;
            }

            public int Count { get; }

            public IEnumerator<double> GetEnumerator() => new Enumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        }

        public static bool TryGet<T>(this T[] array, int index, out T element)
        {
            if (index < 0 || index >= array.Length)
            {
                element = default;
                return false;
            }
            element = array[index];
            return true;
        }

        public static T[] AsArray<T>(this T obj) => new [] { obj };

        public static T[] Initialize<T>(long size) where T : class, new() => Initialize(size, i => new T());

        public static T[] Initialize<T>(long size, Func<int, T> initFunc)
        {
            var array = new T[size];
            array.Initialize(initFunc);
            return array;
        }

        public static void Initialize<T>(this T[] array, Func<int, T> initFunc)
        {
            for (var i = 0; i < array.Length; i++)
                array[i] = initFunc(i);
        }

        public static bool IsNotEmpty(this Array array) => array.Length != 0;

        public static bool IsEmpty(this Array array) => array.Length == 0;

        public static T[] Empty2Null<T>(this T[] array) => array.Length == 0 ? null : array;

        public static bool IsNullOrEmpty(Array array) => array == null || array.Length == 0;

        public static int WriteValues<T>(this T[] array, IEnumerable<T> values, int startIndex = 0)
        {
            foreach (var value in values)
                array[startIndex++] = value;
            return startIndex;
        }

        public static void Reverse<T>(this T[] array) => Reverse(array, 0, array.Length);

        public static void Reverse<T>(this T[] array, int start, int length)
        {
            for (var i = 0; i < length / 2; i++)
                Swap(array, start + length - 1 - i, start + i);
        }

        public static void Swap<T>(this T[] array, int i1, int i2)
        {
            if (i1 == i2)
                return;
            var tmp = array[i1];
            array[i1] = array[i2];
            array[i2] = tmp;
        }

        public static T[] SingletonArray<T>(this T val) => new[] {val};

        public static int[] SortedIndices<T>(this T[] array, Comparer<T> comparer)
        {
            if (array == null) return null;
            if (array.Length == 0) return EmptyArray<int>.Instance;
            var indices = Initialize(array.Length, Functions.Identity);
            Array.Sort(indices, array, comparer);
            return indices;
        }

        public static double NaN2Zero(this double val) => double.IsNaN(val) ? 0 : val; 

        public static IReadOnlyCollection<int> Ints(int start, bool includeStart, int end, bool includeEnd) =>
            new IntRange(start, includeStart, end, includeEnd);

        public static IReadOnlyCollection<double> Doubles(double start, bool includeStart, double end, bool includeEnd, double step) =>
            new DoubleRange(start, includeStart, end, includeEnd, step);

    }
}
