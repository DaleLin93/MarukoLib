using JetBrains.Annotations;
using MarukoLib.Lang;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

            public override string ToString() => $"{Path.GetFileName(SrcPath)} => {Path.GetFileName(DstPath)}";

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
        /// Virual file system with state cache.
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

        public const string AllFileFilter = "All Files (*.*)|*.*";

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

        public static string GetFileFilter([CanBeNull] string desc, [CanBeNull] string ext)
        {
            ext = ext?.Trim();
            if (string.IsNullOrEmpty(ext)) ext = ".*";
            else if (!ext.StartsWith(".", StringComparison.Ordinal)) ext = $".{ext}"; 
            return string.IsNullOrWhiteSpace(desc) ? $"*{ext}|*{ext}" : $"{desc} (*{ext})|*{ext}";
        }

        public static bool IsValidFileName([CanBeNull] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false; 
            foreach (var c in fileName)
                if (!InvalidFileNameChars.Contains(c))
                    return false;
            return true;
        }

        public static string RemoveInvalidCharsForFileName(string fileName)
        {
            var stringBuilder = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
                if (!InvalidFileNameChars.Contains(c))
                    stringBuilder.Append(c);
            fileName = stringBuilder.ToString();
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException();
            return fileName;
        }

        public static string ReplaceInvalidCharsForFileName(string fileName, char replaceWith)
        {
            var stringBuilder = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
                stringBuilder.Append(InvalidFileNameChars.Contains(c) ? replaceWith : c);
            fileName = stringBuilder.ToString();
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException();
            return fileName;
        }

        public static void Rename(out IReadOnlyCollection<RenameFailure> failures, [ItemNotNull] params RenamePair[] input)
            => Rename(input, out failures);

        public static void Rename([NotNull, ItemNotNull] IEnumerable<RenamePair> input, 
            [NotNull, ItemNotNull] out IReadOnlyCollection<RenameFailure> renameFailures)
        {
            renameFailures = EmptyArray<RenameFailure>.Instance;

            /* Preconditions */
            var items = new LinkedList<RenamePair>(input);
            if (items.Count <= 0) return;


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
                /* Check conflicts with unaffacted files */
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

                    /* Process remaining remaining items (renaming loops) */
                    if (items.Count > 0)
                    {
                        var item = items.First.Value;
                        var srcParent = Path.GetDirectoryName(item.SrcPath);
                        string intermediatePath;
                        do
                        {
                            var intermediateName = RemoveInvalidCharsForFileName(Guid.NewGuid() + ".imp");
                            intermediatePath = NormalizePath(Path.Combine(srcParent, intermediateName));
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
            renameFailures = failures.AsReadonly();
        }

    }
}
