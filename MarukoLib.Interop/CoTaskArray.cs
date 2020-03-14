using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MarukoLib.Interop
{

    public static class CoTaskArrays
    {

        public static CoTaskArray<byte> Of(byte[] array)
        {
            var coTaskArray = new CoTaskArray<byte>(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<short> Of(short[] array)
        {
            var coTaskArray = new CoTaskArray<short>(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<int> Of(int[] array) 
        {
            var coTaskArray = new CoTaskArray<int>(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<long> Of(long[] array)
        {
            var coTaskArray = new CoTaskArray<long>(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<float> Of(float[] array)
        {
            var coTaskArray = new CoTaskArray<float>(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static CoTaskArray<double> Of(double[] array)
        {
            var coTaskArray = new CoTaskArray<double>(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

    }

    public sealed class CoTaskArray<T> : IDisposable where T : struct
    {

        public static readonly int ElementSize = Marshal.SizeOf(typeof(T));

        private readonly bool _autoRelease;

        private IntPtr _ptr;

        public CoTaskArray(int length, bool autoRelease = true) : this(Marshal.AllocCoTaskMem(length * ElementSize), length, autoRelease) { }

        public CoTaskArray(IntPtr ptr, int length, bool autoRelease = false)
        {
            _ptr = ptr;
            Length = length;
            _autoRelease = autoRelease;
        }

        ~CoTaskArray() => Dispose();

        public IntPtr Ptr => Interlocked.CompareExchange(ref _ptr, IntPtr.Zero, IntPtr.Zero);

        public bool IsReleased => Ptr == IntPtr.Zero;

        public int Length { get; }

        public void Dispose()
        {
            if (!_autoRelease) return;
            var ptr = Interlocked.Exchange(ref _ptr, IntPtr.Zero);
            if (ptr == IntPtr.Zero) return;
            Marshal.FreeCoTaskMem(Ptr);
        }

    }

}
