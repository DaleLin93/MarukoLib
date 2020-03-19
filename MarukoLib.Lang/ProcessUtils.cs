using System.Diagnostics;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{
    public static class ProcessUtils
    {

        [NotNull] public static Process Me { get; } = Process.GetCurrentProcess();

        public static void Suicide() => Me.Kill();

        [CanBeNull]
        public static Process Restart([NotNull] this Process process)
        {
            if (!process.HasExited) process.Kill();
            return Process.Start(process.StartInfo);
        }

    }

}
