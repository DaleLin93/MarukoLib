﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarukoLib.Lang
{
    public static class StringUtils
    {

        public static bool IsNotEmpty(this string str) => !str.IsEmpty();

        public static bool IsEmpty(this string str) => str.Length <= 0;

        public static bool IsNotBlank(this string str) => !str.IsBlank();

        public static bool IsBlank(this string str)
        {
            if (str.IsEmpty())
                return true;
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

        public static int IndexOf(this string self, string value, int startIndex)
        {
            if (value.IsEmpty()) return -1;
            var firstChar = value[0];
            while (true)
            {
                var indexOf = self.IndexOf(firstChar, startIndex);
                if (indexOf == -1) return -1;
                if (self.Length - indexOf < value.Length) return -1;
                var flag = true;
                for (var i = 0; i < value.Length; i++)
                    if (self[indexOf + i] != value[i])
                    {
                        flag = false;
                        break;
                    }
                if (flag) return indexOf;
            }
        }

        public static string Trim2Null(this string str)
        {
            str = str.Trim();
            return str.IsEmpty() ? null : str;
        }

        public static bool TryTrim(this string str, string start, string end, out string result, StringComparison comparison = StringComparison.Ordinal)
        {
            var startIndex = 0;
            var length = str.Length;
            if (start != null && !start.IsEmpty() && str.StartsWith(start, comparison))
            {
                startIndex += start.Length;
                length -= start.Length;
            }
            if (end != null && !end.IsEmpty() && str.EndsWith(end, comparison))
                length -= end.Length;
            if (length == str.Length)
            {
                result = str;
                return false;
            }
            else
            {
                result = str.Substring(startIndex, length);
                return true;
            }
        }

        public static string Trim(this string str, string start, string end, StringComparison comparison = StringComparison.Ordinal)
        {
            TryTrim(str, start, end, out var result, comparison);
            return result;
        }

        public static string TrimStart(this string str, string trim, StringComparison comparison = StringComparison.Ordinal)
        {
            if (trim == null || trim.IsEmpty() || !str.StartsWith(trim, comparison)) return str;
            return str.Substring(trim.Length);
        }

        public static string TrimEnd(this string str, string trim, StringComparison comparison = StringComparison.Ordinal)
        {
            if (trim == null || trim.IsEmpty() || !str.EndsWith(trim, comparison)) return str;
            return str.Substring(0, str.Length - trim.Length);
        }

        public static string[] GetLines(this string str) => Regex.Split(str, "\r\n|\r|\n");

        public static string GetFirstLine(this string str)
        {
            var index = str.IndexOf("\r\n", StringComparison.Ordinal);
            if (index == -1)
                index = str.IndexOf('\n');
            return index == -1 ? str : str.Substring(0, index);
        }

        public static string Replace(this string str, IEnumerable<KeyValuePair<string, string>> replaces) =>
            replaces.Aggregate(str, (current, pair) => current.Replace(pair.Key, pair.Value));

        public static decimal LevenshteinDistancePercent(string str1, string str2)
        {
            var val = LevenshteinDistance(str1, str2);
            return 1 - (decimal)val / System.Math.Max(str1.Length, str2.Length);
        }

        public static int LevenshteinDistance(string str1, string str2)
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

        private static int Min(int first, int second, int third) => System.Math.Min(System.Math.Min(first, second), third);

    }
}