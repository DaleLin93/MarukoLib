using System;
using JetBrains.Annotations;

namespace MarukoLib.IO
{

    public sealed class PathInfo
    {

        public PathInfo([NotNull] string path) => Path = path ?? throw new ArgumentNullException(nameof(path));

        [CanBeNull] public static PathInfo Of([CanBeNull] string path) => path == null ? null : new PathInfo(path);

        [NotNull] public string Path { get; }

        [NotNull] public string FullPath => System.IO.Path.GetFullPath(Path);

        [CanBeNull] public string Directory => System.IO.Path.GetDirectoryName(FullPath);

    }

    public sealed class PathHolder
    {

        public class ChangeEventArgs : EventArgs
        {

            public ChangeEventArgs([CanBeNull] string newValue) => NewValue = newValue;

            [CanBeNull] public string NewValue { get; }

            public bool Cancelled { get; set; }

        }

        public event EventHandler<ChangeEventArgs> Change;

        public PathHolder() : this(null) { }

        public PathHolder([CanBeNull] string defaultPath) : this(defaultPath, null) { }

        public PathHolder([CanBeNull] string defaultPath, [CanBeNull] string path)
        {
            DefaultPath = defaultPath;
            PathInfo = PathInfo.Of(path);
        }

        [CanBeNull] public string DefaultPath { get; }

        [CanBeNull] public PathInfo PathInfo { get; private set; }

        [CanBeNull]
        public string Path
        {
            get => PathInfo?.Path;
            set => SetPath(value);
        }

        [CanBeNull] public string PathOrDefault => Path ?? DefaultPath;

        public bool IsValueAbsent => PathInfo == null;

        public bool SetPathOrDefault([CanBeNull] string path, bool raiseEvents = true) => SetPath(Path = path ?? DefaultPath, raiseEvents);

        public bool SetPath([CanBeNull] string path, bool raiseEvents = true)
        {
            if (Equals(path, Path)) return false;
            if (raiseEvents)
            {
                RaiseValueChangedEvent(path, out var cancelled);
                if (cancelled) return false;
            }
            PathInfo = PathInfo.Of(path);
            return true;
        }

        private void RaiseValueChangedEvent(string val, out bool cancelled)
        {
            var eventArgs = new ChangeEventArgs(val);
            Change?.Invoke(this, eventArgs);
            cancelled = eventArgs.Cancelled;
        }

    }

}
