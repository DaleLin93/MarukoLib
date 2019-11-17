using System;
using System.Runtime.InteropServices;

namespace MarukoLib.Interop
{

    public static class CoTaskArrays
    {

        public static CoTaskArray<byte> Of(byte[] array)
        {
            var coTaskArray = CoTaskArray<byte>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<short> Of(short[] array)
        {
            var coTaskArray = CoTaskArray<short>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<int> Of(int[] array) 
        {
            var coTaskArray = CoTaskArray<int>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<long> Of(long[] array)
        {
            var coTaskArray = CoTaskArray<long>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<float> Of(float[] array)
        {
            var coTaskArray = CoTaskArray<float>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<double> Of(double[] array)
        {
            var coTaskArray = CoTaskArray<double>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

    }

    public sealed class CoTaskArray<T> : IDisposable where T : struct
    {

        public static readonly int ElementSize = Marshal.SizeOf(typeof(T));

        private readonly bool _autoRelease;

        public CoTaskArray(IntPtr ptr, int length, bool autoRelease = false)
        {
            Ptr = ptr;
            Length = length;
            _autoRelease = autoRelease;
        }

        public static CoTaskArray<T> Alloc(int len) => new CoTaskArray<T>(Marshal.AllocCoTaskMem(len * ElementSize), len, true);

        public IntPtr Ptr { get; private set; }

        public int Length { get; }

        public void Dispose()
        {
            if (!_autoRelease || Ptr == IntPtr.Zero) return;
            Marshal.FreeCoTaskMem(Ptr);
            Ptr = IntPtr.Zero;
        }

    }

}
