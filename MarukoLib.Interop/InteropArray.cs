using System;
using System.Runtime.InteropServices;

namespace MarukoLib.Interop
{

    public static class InteropArrays
    {

        public static InteropArray<byte> Of(byte[] array)
        {
            var coTaskArray = InteropArray<byte>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static InteropArray<short> Of(short[] array)
        {
            var coTaskArray = InteropArray<short>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static InteropArray<int> Of(int[] array) 
        {
            var coTaskArray = InteropArray<int>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static InteropArray<long> Of(long[] array)
        {
            var coTaskArray = InteropArray<long>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static InteropArray<float> Of(float[] array)
        {
            var coTaskArray = InteropArray<float>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

        public static InteropArray<double> Of(double[] array)
        {
            var coTaskArray = InteropArray<double>.Alloc(array.Length);
            Marshal.Copy(array, 0, coTaskArray.Ptr, array.Length);
            return coTaskArray;
        }

    }

    public sealed class InteropArray<T> : IDisposable where T : struct
    {

        public static readonly int ElementSize = Marshal.SizeOf(typeof(T));

        private readonly bool _autoRelease;

        public InteropArray(IntPtr ptr, int length, bool autoRelease = false)
        {
            Ptr = ptr;
            Length = length;
            _autoRelease = autoRelease;
        }

        public static InteropArray<T> Alloc(int len) => new InteropArray<T>(Marshal.AllocCoTaskMem(len * ElementSize), len, true);

        public IntPtr Ptr { get; private set; }

        public int Length { get; }

        public void Dispose()
        {
            if (_autoRelease && Ptr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(Ptr);
                Ptr = IntPtr.Zero;
            }
        }

    }

}
