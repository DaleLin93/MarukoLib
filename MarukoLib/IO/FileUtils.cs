using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.IO
{

    public static class FileUtils
    {

        public sealed class RenamePair
        {

            public RenamePair(string parent, string srcName, string dstName, bool directory)
            {
                if (!IsValidFileName(srcName)) throw new ArgumentException($"Invalid src name: '{srcName}'");
                if (!IsValidFileName(dstName)) throw new ArgumentException($"Invalid dst name: '{dstName}'");
                IsDirectory = directory;
                SrcPath = NormalizePath(parent, srcName);
                DstPath = NormalizePath(parent, dstName);
            }

            public RenamePair(string srcPath, string dstPath, bool directory)
            {
                IsDirectory = directory;
                SrcPath = NormalizePath(srcPath);
                DstPath = NormalizePath(dstPath);
            }

            public RenamePair(string srcPath, string dstPath)
            {
                SrcPath = NormalizePath(srcPath);
                DstPath = NormalizePath(dstPath);
                IsDirectory = CheckIsDirectory(srcPath);
            }

            public static bool CheckIsDirectory(string path)
            {
                if (Directory.Exists(path)) return true;
                if (File.Exists(path)) return false;
                throw new Exception($"Given path is not a file or a directory: '{path}'");
            }

            public bool IsDirectory { get; }

            public string SrcPath { get; }

            public string DstPath { get; }

            public void Rename()
            {
                if (IsDirectory)
                    Directory.Move(SrcPath, DstPath);
                else
                    File.Move(SrcPath, DstPath);
            }

            public override string ToString()
            {
                var src = Path.GetFileName(SrcPath);
                var dst = Path.GetFileName(DstPath);
                return IsDirectory ? $"[ {src} ] => [ {dst} ]" : $"{src} => {dst}";
            }

        }

        public sealed class RenameFailure
        {

            public RenameFailure(RenamePair renamePair, Exception exception)
            {
                RenamePair = renamePair;
                Exception = exception;
            }

            public RenamePair RenamePair { get; }

            public Exception Exception { get; }

        }

        /// <summary>
        /// Virtual file system with state cache.
        /// </summary>
        internal class VirtualFileSystem
        {

            private readonly IDictionary<string, bool> _pathStates = new Dictionary<string, bool>();

            public static bool IsExistsInFileSystem(string path) => Directory.Exists(path) || File.Exists(path);

            public bool Rename(RenamePair item) => Rename0(item.SrcPath, item.DstPath);

            public bool Rename(string srcPath, string dstPath)
            {
                /* Path normalizations */
                srcPath = NormalizePath(srcPath);
                dstPath = NormalizePath(dstPath);
                /* Do rename */
                return Rename0(srcPath, dstPath);
            }

            public bool IsSrcExists(RenamePair item) => Exists0(item.SrcPath);

            public bool IsDstExists(RenamePair item) => Exists0(item.DstPath);

            public bool Exists(string path) => Exists0(NormalizePath(path));

            internal bool Exists0(string normPath)
            {
                if (_pathStates.TryGetValue(normPath, out var state)) return state; /* Check modifications on the fly */
                return _pathStates[normPath] = IsExistsInFileSystem(normPath); /* Cache it? */
            }

            internal bool Rename0(string normSrcPath, string normDstPath)
            {
                /* Preconditions */
                if (!Exists0(normSrcPath)) return false;
                if (Exists0(normDstPath)) return false;
                /* Modifications */
                _pathStates[normSrcPath] = false;
                _pathStates[normDstPath] = true;
                return true;
            }

        }

        public const string AllFileFilterPattern = "All Files (*.*)|*.*";

        private static readonly ISet<char> InvalidFileNameChars;

        static FileUtils()
        {
            InvalidFileNameChars = new HashSet<char>();
            foreach (var ch in Path.GetInvalidFileNameChars())
                InvalidFileNameChars.Add(ch);
        }

        public static string ExecutableDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string NormalizePath(string parent, string name) => Path.Combine(parent, name);

        public static string NormalizePath(string path) => Path.GetFullPath(path);

        public static string NormalizeSuffix(string suffix) => suffix == null ? null : suffix.Length > 0 && suffix[0] != '.' ? $".{suffix}" : suffix;

        public static string MergeFilterPatterns([CanBeNull, ItemCanBeNull] params string[] patterns) 
            => patterns?.Select(StringUtils.Trim2Null).NotNull().Distinct()
                .Aggregate<string, string>(null, (a, b) => a == null ? b : $"{a}|{b}");

        public static string GetFileFilterPattern([CanBeNull] string desc, [CanBeNull, ItemCanBeNull] params string[] extensions)
        {
            var extFilter = extensions?.Select(StringUtils.Trim2Null).NotNull()
                .Select(ext => $"*{NormalizeSuffix(ext)}").Distinct().Aggregate<string, string>(null, (a, b) => a == null ? b : $"{a},{b}");
            if (string.IsNullOrEmpty(extFilter)) extFilter = "*.*";
            return string.IsNullOrWhiteSpace(desc) ? $"{extFilter}|{extFilter}" : $"{desc} ({extFilter})|{extFilter}";
        }

        public static bool IsValidFileName([CanBeNull] string fileName) 
            => !string.IsNullOrWhiteSpace(fileName) && fileName.All(c => InvalidFileNameChars.Contains(c));

        public static string RemoveInvalidCharsForFileName(string fileName, bool checkValid = false)
        {
            var stringBuilder = new StringBuilder(fileName.Length);
            foreach (var c in fileName.Where(c => !InvalidFileNameChars.Contains(c)))
                stringBuilder.Append(c);
            fileName = stringBuilder.ToString();
            if (checkValid && string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException();
            return fileName;
        }

        public static string ReplaceInvalidCharsForFileName(string fileName, char replaceWith, bool checkValid = false)
        {
            var stringBuilder = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
                stringBuilder.Append(InvalidFileNameChars.Contains(c) ? replaceWith : c);
            fileName = stringBuilder.ToString();
            if (checkValid && string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException();
            return fileName;
        }

        public static long GetFileLength(string file) => new FileInfo(file).Length;

        public static bool DeleteIfBlank(string file, long fileSizeThreshold, Encoding encoding = null)
        {
            if (file == null) return false;
            if (File.Exists(file) && GetFileLength(file) < fileSizeThreshold)
            {
                var text = File.ReadAllText(file, encoding ?? Encoding.Default);
                if (string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        File.Delete(file);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Rename multiple files or directories.
        /// </summary>
        /// <param name="paths">Param layout: [src path 0, dst path 0, src path 1, dst path 1, ...].</param>
        /// <returns>Failures of renaming.</returns>
        public static IReadOnlyCollection<RenameFailure> Rename(params string[] paths)
        {
            if (paths.Length % 2 != 0) throw new Exception("The input paths must be paired");
            var list = new List<RenamePair>(paths.Length / 2);
            for (var i = 0; i < paths.Length; i+=2)
                list.Add(new RenamePair(paths[i], paths[i + 1]));
            return Rename(list);
        }

        /// <summary>
        /// Rename multiple files or directories.
        /// </summary>
        /// <param name="input">Rename pairs.</param>
        /// <returns>Failures of renaming.</returns>
        public static IReadOnlyCollection<RenameFailure> Rename([NotNull, ItemNotNull] IEnumerable<RenamePair> input)
        {
            /* Preconditions */
            var items = new LinkedList<RenamePair>(input);
            if (items.Count <= 0) return EmptyArray<RenameFailure>.Instance;

            /* Checking conflicts */
            var vfs = new VirtualFileSystem();
            {
                /* Initialize caches */
                var srcSet = new HashSet<string>();
                var dstSet = new HashSet<string>();
                /* Check duplications */
                foreach (var item in items)
                {
                    if (!srcSet.Add(item.SrcPath)) throw new Exception($"File '{item.SrcPath}' will be renamed multiple times!");
                    if (!dstSet.Add(item.DstPath)) throw new Exception($"Multiple files will be renamed to '{item.DstPath}'!");
                }
                /* Check conflicts with unaffected files */
                foreach (var item in items)
                {
                    if (!vfs.IsSrcExists(item) && !dstSet.Contains(item.SrcPath))
                        throw new Exception($"File '{item.SrcPath}' does not exist to be renamed!");
                    if (vfs.IsDstExists(item) && !srcSet.Contains(item.DstPath))
                        throw new Exception($"File '{item.DstPath}' is already exists!");
                }
            }

            /* Sort to get a properly renaming order */
            var sortedItems = new List<RenamePair>(items.Count * 2);
            {
                var routedItems = new LinkedList<RenamePair>();

                do
                {
                    /* Topology sort */
                    bool changed;
                    do
                    {
                        changed = false;
                        var node = items.First;
                        while (node != null)
                        {
                            var next = node.Next;
                            var item = node.Value;
                            if (vfs.Rename(item))
                            {
                                sortedItems.Add(item);
                                items.Remove(node);
                                changed = true;
                            }
                            node = next;
                        }
                    }
                    while (changed && items.Count > 0);

                    /* Process remaining items, break loops */
                    if (items.Count > 0)
                    {
                        var item = items.First.Value;
                        var srcParent = Path.GetDirectoryName(item.SrcPath);
                        System.Diagnostics.Debug.Assert(srcParent != null); // Parent directory must be non-null after path normalization.
                        string intermediatePath;
                        do
                        {
                            intermediatePath = NormalizePath(Path.Combine(srcParent, RemoveInvalidCharsForFileName(Guid.NewGuid() + ".imp")));
                        }
                        while (!vfs.Rename(item.SrcPath, intermediatePath));
                        sortedItems.Add(new RenamePair(item.SrcPath, intermediatePath, item.IsDirectory));
                        routedItems.AddLast(new RenamePair(intermediatePath, item.DstPath, item.IsDirectory));
                        items.RemoveFirst();
                    }
                }
                while (items.Count > 0);

                sortedItems.AddRange(routedItems);
            }

            /* Do rename */
            var failures = new LinkedList<RenameFailure>();
            foreach (var item in sortedItems)
                try
                {
                    item.Rename();
                }
                catch (Exception ex)
                {
                    failures.AddLast(new RenameFailure(item, ex));
                }
            return failures.AsReadonly();
        }

    }
}
