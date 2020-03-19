using System.IO;
using System.Runtime.InteropServices;
using MarukoLib.Interop.Properties;

namespace MarukoLib.Interop
{

    public static class InpOutX64
    {

        public const string DllName = "inpoutx64.dll";

        private static byte[] DllBinary => Resources.inpoutx64;

        [DllImport(DllName)]
        public static extern void Out32(short portAddress, short value);

        [DllImport(DllName)]
        public static extern char Inp32(short portAddress);

        public static void ExtractDllToWorkDir() => ExtractDllToFile(DllName);

        public static void ExtractDllToFile(string file)
        {
            if (File.Exists(file) && new FileInfo(file).Length == DllBinary.Length) return;
            using (var stream = new FileStream(file, FileMode.OpenOrCreate))
                WriteDll(stream);
        }

        public static void WriteDll(Stream stream)
        {
            var bytes = DllBinary;
            stream.Write(bytes, 0, bytes.Length);
        }

    }

}
