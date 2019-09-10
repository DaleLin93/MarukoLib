namespace MarukoLib.Lang
{
    public static class ProcessUtils
    {

        public static void Suicide() => System.Diagnostics.Process.GetCurrentProcess().Kill();

    }

}
