﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{
    public static class StringUtils
    {

        public const StringComparison StrictStrComp= StringComparison.Ordinal;

        public static bool IsNullOrBlank([CanBeNull] string str) => str == null || IsBlank(str);

        public static bool IsNotEmpty([NotNull] this string str) => !str.IsEmpty();

        public static bool IsEmpty([NotNull] this string str) => str.Length <= 0;

        public static bool IsNotBlank([NotNull] this string str) => !str.IsBlank();

        public static bool IsBlank([NotNull] this string str)
        {
            if (str.IsEmpty()) return true;
            foreach (var c in str)
                switch (c)
                {
                    case '\n':
                    case '\r':
                    case ' ':
                        continue;
                    default:
                        return false;
                }
            return true;
        }

        public static string ToPascalCase([NotNull] this string str, [CanBeNull] CultureInfo culture = null) =>
            string.Concat(char.ToUpper(str[0], culture ?? CultureInfo.CurrentCulture), str.Substring(1));

        public static string ToCamelCase([NotNull] this string str, [CanBeNull] CultureInfo culture = null) =>
            string.Concat(char.ToLower(str[0], culture ?? CultureInfo.CurrentCulture), str.Substring(1));

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public static string TrimOrPadLeft([CanBeNull] this string str, int length, char pad)
        {
            if (str == null) return new string(pad, length);
            if (str.Length == length) return str;
            if (str.Length > length) return str.Substring(str.Length - length, length);
            return str.PadLeft(length, pad);
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public static string TrimOrPadRight([CanBeNull] this string str, int length, char pad)
        {
            if (str == null) return new string(pad, length);
            if (str.Length == length) return str;
            if (str.Length > length) return str.Substring(0, length);
            return str.PadRight(length, pad);
        }

        public static int IndexOf([NotNull] this string self, [CanBeNull] string value, int startIndex)
        {
            if (string.IsNullOrEmpty(value)) return -1;
            var firstChar = value[0];
            for (;;)
            {
                var indexOf = self.IndexOf(firstChar, startIndex);
                if (indexOf == -1) return -1;
                if (self.Length - indexOf < value.Length) return -1;
                if (!value.Where((t, i) => self[indexOf + i] != t).Any()) return indexOf;
            }
        }

        public static string Trim2Null([NotNull] this string str) => (str = str.Trim()).IsEmpty() ? null : str;

        public static string Trim2Null([NotNull] this string str, params char[] chars) => (str = str.Trim(chars)).IsEmpty() ? null : str;

        public static bool IsPartlyEqual(this string str, int startIndex, string target, StringComparison comparison = StrictStrComp) =>
            startIndex >= 0 && startIndex <= str.Length - target.Length && string.Compare(str, startIndex, target, 0, target.Length, comparison) == 0;

        public static bool TryTrim([NotNull] this string str, [CanBeNull] string start, [CanBeNull] string end, 
            out string result, bool multiple = true, StringComparison comparison = StrictStrComp)
        {
            var startIndex = 0;
            var endIndex = str.Length;
            if (!string.IsNullOrEmpty(start))
                while (IsPartlyEqual(str, startIndex, start, comparison))
                {
                    startIndex += start.Length;
                    if (!multiple) break;
                }
            if (!string.IsNullOrEmpty(end))
                while (IsPartlyEqual(str, endIndex - end.Length, end, comparison))
                {
                    endIndex -= end.Length;
                    if (!multiple) break;
                }
            var length = endIndex - startIndex;
            if (length == str.Length)
            {
                result = str;
                return false;
            }
            result = str.Substring(startIndex, length);
            return true;
        }

        public static bool TryTrimStart([NotNull] this string str, [CanBeNull] string trim, 
            out string result, bool multiple = true, StringComparison comparison = StrictStrComp)
        {
            var trimCount = 0;
            if (!string.IsNullOrEmpty(trim))
                while (IsPartlyEqual(str, trimCount, trim, comparison))
                {
                    trimCount += trim.Length;
                    if (!multiple) break;
                }
            if (trimCount == 0)
            {
                result = str;
                return false;
            }
            result = str.Substring(trimCount);
            return true;
        }

        public static bool TryTrimEnd([NotNull] this string str, [CanBeNull] string trim, 
            out string result, bool multiple = true, StringComparison comparison = StrictStrComp)
        {
            var trimCount = 0;
            if (!string.IsNullOrEmpty(trim))
                // ReSharper disable once PossibleNullReferenceException
                while (IsPartlyEqual(str, str.Length - trimCount - trim.Length, trim, comparison))
                {
                    trimCount += trim.Length;
                    if (!multiple) break;
                }
            if (trimCount == 0)
            {
                result = str;
                return false;
            }
            result = str.Substring(0, str.Length - trimCount);
            return true;
        }

        public static string Trim([NotNull] this string str, [CanBeNull] string start, [CanBeNull] string end, bool multiple = true, StringComparison comparison = StrictStrComp) => 
            TryTrim(str, start, end, out var result, multiple, comparison) ? result : str;

        public static string TrimStart([NotNull] this string str, [CanBeNull] string trim, bool multiple = true, StringComparison comparison = StrictStrComp) => 
             TryTrimStart(str, trim, out var result, multiple, comparison) ? result : str;

        public static string TrimEnd([NotNull] this string str, [CanBeNull] string trim, bool multiple = true, StringComparison comparison = StrictStrComp) =>
            TryTrimEnd(str, trim, out var result, multiple, comparison) ? result : str;

        public static string[] GetLines([NotNull] this string str) => Regex.Split(str, "\r\n|\r|\n");

        public static string GetFirstLine([NotNull] this string str)
        {
            var index = str.IndexOf("\r\n", StringComparison.Ordinal);
            if (index == -1) index = str.IndexOf('\n');
            return index == -1 ? str : str.Substring(0, index);
        }

        public static string Replace([NotNull] this string str, [NotNull] IEnumerable<KeyValuePair<string, string>> replaces) =>
            replaces.Aggregate(str, (current, pair) => current.Replace(pair.Key, pair.Value));

        public static StringBuilder AppendIfEmpty([NotNull] this StringBuilder stringBuilder, [CanBeNull] object value)
        {
            if (stringBuilder.Length == 0) stringBuilder.Append(value);
            return stringBuilder;
        }

        public static string EncodeBase64([CanBeNull] this string source, [CanBeNull] Encoding encoding = null)
            => source == null ? null : Convert.ToBase64String((encoding ?? Encoding.Default).GetBytes(source));

        public static string DecodeBase64([CanBeNull] this string encoded, [CanBeNull] Encoding encoding = null)
            => encoded == null ? null : (encoding ?? Encoding.Default).GetString(Convert.FromBase64String(encoded));

        public static decimal LevenshteinDistancePercent([NotNull] string str1, [NotNull] string str2) => 
            1 - (decimal)LevenshteinDistance(str1, str2) / Math.Max(str1.Length, str2.Length);

        public static int LevenshteinDistance([NotNull] string str1, [NotNull] string str2)
        {
            var n = str1.Length;
            var m = str2.Length;

            int i, j;
            if (n == 0)
                return m;
            if (m == 0)
                return n;

            var matrix = new int[n + 1, m + 1];
            for (i = 0; i <= n; i++)
                matrix[i, 0] = i;

            for (j = 0; j <= m; j++)
                matrix[0, j] = j;

            for (i = 1; i <= n; i++)
            {
                var ch1 = str1[i - 1];
                for (j = 1; j <= m; j++)
                {
                    var ch2 = str2[j - 1];
                    var temp = Equals(ch1, ch2) ? 0 : 1;
                    matrix[i, j] = Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1, matrix[i - 1, j - 1] + temp);
                }
            }
            return matrix[n, m];
        }

        private static int Min(int first, int second, int third) => Math.Min(Math.Min(first, second), third);

    }
}
