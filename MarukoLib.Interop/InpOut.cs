using System;
using System.IO;
using System.Runtime.InteropServices;
using MarukoLib.Interop.Properties;

namespace MarukoLib.Interop
{

    public abstract class InpOut
    {

        public static InpOut Instance => IntPtr.Size == 4 ? (InpOut) InpOut32.Instance : InpOutX64.Instance; 

        public abstract string LibName { get; }

        public abstract byte[] LibBinary { get; }

        public abstract short Read(short address);

        public abstract void Write(short address, short value);

        public void ExtractLibraryToWorkDir() => ExtractLibraryToFile(LibName);

        public void ExtractLibraryToFile(string file)
        {
            if (File.Exists(file) && new FileInfo(file).Length == LibBinary.Length) return;
            using (var stream = new FileStream(file, FileMode.OpenOrCreate))
                WriteLibrary(stream);
        }

        public void WriteLibrary(Stream stream)
        {
            var bytes = LibBinary;
            stream.Write(bytes, 0, bytes.Length);
        }

    }

    public class InpOut32 : InpOut
    {

        public const string DllName = "inpout32.dll";

        public new static readonly InpOut32 Instance = new InpOut32();

        [DllImport(DllName)]
        public static extern short Inp32(short address);

        [DllImport(DllName)]
        public static extern void Out32(short address, short value);

        private InpOut32() { }

        public override string LibName => DllName;

        public override byte[] LibBinary => Resources.inpout32;

        public override short Read(short address) => Inp32(address);

        public override void Write(short address, short value) => Out32(address, value);

    }

    public class InpOutX64 : InpOut
    {

        public const string DllName = "inpoutx64.dll";
        
        public new static readonly InpOutX64 Instance = new InpOutX64();

        [DllImport(DllName)]
        public static extern short Inp32(short portAddress);

        [DllImport(DllName)]
        public static extern void Out32(short portAddress, short value);

        private InpOutX64() { }

        public override string LibName => DllName;

        public override byte[] LibBinary => Resources.inpoutx64;

        public override short Read(short address) => Inp32(address);

        public override void Write(short address, short value) => Out32(address, value);

    }

}
