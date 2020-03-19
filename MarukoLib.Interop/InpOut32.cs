using System.IO;
using System.Runtime.InteropServices;
using MarukoLib.Interop.Properties;

namespace MarukoLib.Interop
{

    public static class InpOut32
    {

        public const string DllName = "inpout32.dll";

        private static byte[] DllBinary => Resources.inpout32;

        [DllImport(DllName)]
        public static extern void Out32(short address, short value);

        [DllImport(DllName)]
        public static extern byte Inp32(short address);

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
