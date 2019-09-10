using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MarukoLib.IO
{

    public static class FileUtils
    {

        public const string AllFileFilter = "All Files (*.*)|*.*";

        private static readonly ISet<char> InvalidFileNameChars;

        static FileUtils()
        {
            InvalidFileNameChars = new HashSet<char>();
            foreach (var ch in Path.GetInvalidFileNameChars())
                InvalidFileNameChars.Add(ch);
        }

        public static string ExecutableDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string GetFileFilter(string desc, string ext)
        {
            ext = ext?.Trim();
            if (string.IsNullOrEmpty(ext)) ext = ".*";
            else if (!ext.StartsWith(".", StringComparison.Ordinal)) ext = $".{ext}"; 
            return string.IsNullOrWhiteSpace(desc) ? $"*{ext}|*{ext}" : $"{desc} (*{ext})|*{ext}";
        }

        public static string RemoveInvalidCharacterForFileName(this string fileName, char? replaceWith = null)
        {
            var stringBuilder = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
            {
                if (!InvalidFileNameChars.Contains(c))
                {
                    stringBuilder.Append(c);
                    continue;
                }
                if (replaceWith != null) stringBuilder.Append(replaceWith.Value);
            }
            return stringBuilder.ToString();
        }

    }
}
