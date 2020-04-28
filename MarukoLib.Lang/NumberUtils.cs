using System;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public static class NumberUtils
    {

        #region Is Equal With Tolerance

        public static bool IsEqual(int a, int b, int tolerance) => Math.Abs(a - b) <= Math.Abs(tolerance);

        public static bool IsEqual(long a, long b, long tolerance) => Math.Abs(a - b) <= Math.Abs(tolerance);

        public static bool IsEqual(float a, float b, float tolerance) => Math.Abs(a - b) <= Math.Abs(tolerance);

        public static bool IsEqual(double a, double b, double tolerance) => Math.Abs(a - b) <= Math.Abs(tolerance);

        #endregion

        #region Snap

        public static int Snap(this int value, int target, int tolerance) => IsEqual(value, target, tolerance) ? target : value;

        public static long Snap(this long value, long target, long tolerance) => IsEqual(value, target, tolerance) ? target : value;

        public static float Snap(this float value, float target, float tolerance) => IsEqual(value, target, tolerance) ? target : value;

        public static double Snap(this double value, double target, double tolerance) => IsEqual(value, target, tolerance) ? target : value;

        #endregion

        #region Parse Number

        public static int ParseByte(this string str, byte defaultVal) => byte.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static sbyte ParseSByte(this string str, sbyte defaultVal) => sbyte.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static short ParseShort(this string str, short defaultVal) => short.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static ushort ParseUShort(this string str, ushort defaultVal) => ushort.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static int ParseInt(this string str, int defaultVal) => int.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static uint ParseUInt(this string str, uint defaultVal) => uint.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static long ParseLong(this string str, long defaultVal) => long.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static ulong ParseULong(this string str, ulong defaultVal) => ulong.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static float ParseFloat(this string str, float defaultVal) => float.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static double ParseDouble(this string str, double defaultVal) => double.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static decimal ParseDecimal(this string str, decimal defaultVal) => decimal.TryParse(str, out var parsed) ? parsed : defaultVal;

        #endregion

        #region Parse Number Nullable

        public static int? ParseByte(this string str, byte? defaultVal) => byte.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static sbyte? ParseSByte(this string str, sbyte? defaultVal) => sbyte.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static short? ParseShort(this string str, short? defaultVal) => short.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static ushort? ParseUShort(this string str, ushort? defaultVal) => ushort.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static int? ParseInt(this string str, int? defaultVal = null) => int.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static uint? ParseUInt(this string str, uint? defaultVal) => uint.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static long? ParseLong(this string str, long? defaultVal) => long.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static ulong? ParseULong(this string str, ulong? defaultVal) => ulong.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static float? ParseFloat(this string str, float? defaultVal = null) => float.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static double? ParseDouble(this string str, double? defaultVal = null) => double.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static decimal? ParseDecimal(this string str, decimal? defaultVal = null) => decimal.TryParse(str, out var parsed) ? parsed : defaultVal;

        #endregion

        public static bool IsDigit(this char c) => c >= '0' && c <= '9';

        public static bool IsDigits([CanBeNull] this string str) => !string.IsNullOrEmpty(str) && str.All(IsDigit);

        public static bool IsReal(this double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public static double ConvertNaN(this double val, double val4NaN) => double.IsNaN(val) ? val4NaN : val;

        public static double ConvertNaN2Zero(this double val) => ConvertNaN(val, 0);

        public static void ConvertNaNInPlace(this double[] values, double val4NaN)
        {
            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                if (double.IsNaN(val))
                    values[i] = val4NaN;
            }
        }

        public static void ConvertNaN2ZeroInPlace(this double[] values) => ConvertNaNInPlace(values, 0);

        public static double Min(this double[] values, out int minIndex)
        {
            var minValue = double.NaN;
            minIndex = -1;
            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                if (i == 0 || val < minValue)
                {
                    minValue = val;
                    minIndex = i;
                }
            }
            return minValue;
        }

        public static double Max(this double[] values, out int maxIndex)
        {
            var maxValue = double.NaN;
            maxIndex = -1;
            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                if (i == 0 || val > maxValue)
                {
                    maxValue = val;
                    maxIndex = i;
                }
            }
            return maxValue;
        }

        public static void PercentageInPlace(this double[] values)
        {
            if (values.Length == 0) return;
            var sum = values.Sum();
            for (var i = 0; i < values.Length; i++)
                values[i] = values[i] / sum;
        }

        public static void NormalizationInPlace(this double[] values, double min = -1, double max = 1)
        {
            if (values.Length == 0) return;
            var valMin = double.NaN;
            var valMax = double.NaN;
            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                if (i == 0)
                {
                    valMin = val;
                    valMax = val;
                }
                else
                {
                    if (valMin > val) valMin = val;
                    if (valMax < val) valMax = val;
                }
            }
            var valRange = valMax - valMin;
            var newRange = max - min;
            for (var i = 0; i < values.Length; i++)
                values[i] = (values[i] - valMin) / valRange * newRange + min;
        }

        public static void SoftmaxInPlace(this double[] values, double? temperature = null)
        {
            if (values.Length == 0) return;
            double sum = 0;
            for (var i = 0; i < values.Length; i++)
                sum += values[i] = Math.Exp(values[i] / temperature ?? values[i]);
            for (var i = 0; i < values.Length; i++)
                values[i] = values[i] / sum;
        }

        public static void ZScoreInPlace(this double[] values)
        {
            if (values.Length == 0) return;
            var mean = values.Sum() / values.Length;
            var variance = 0.0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in values)
            {
                var diff = value - mean;
                variance += diff * diff;
            }
            variance /= values.Length;
            var std = Math.Sqrt(variance);
            for (var i = 0; i < values.Length; i++)
                values[i] = (values[i] - mean) / std;
        }

    }
}
