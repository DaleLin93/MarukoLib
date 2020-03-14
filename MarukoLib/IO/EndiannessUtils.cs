using System;
using System.Collections.Generic;
using System.Threading;
using MarukoLib.Lang;

namespace MarukoLib.IO
{

    public enum Endianness
    {
        LittleEndian,
        BigEndian
    }

    public static class EndiannessUtils
    {

        public const Endianness NetworkOrder = Endianness.BigEndian;

        public static readonly Endianness SystemByteOrder = BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

        private static readonly ThreadLocal<byte[]>[] BufferThreadLocals = new ThreadLocal<byte[]>[sizeof(long)];

        private static readonly ReaderWriterLockSlim BufferLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public static byte InvertBits(this byte b) => (byte)~b;

        public static byte ReverseBits(this byte b)
        {
            b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
            b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
            b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
            return b;
        }

        public static short ReadInt16(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(short), BitConverter.ToInt16);

        public static ushort ReadUInt16(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(ushort), BitConverter.ToUInt16);

        public static int ReadInt32(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(int), BitConverter.ToInt32);

        public static uint ReadUInt32(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(uint), BitConverter.ToUInt32);

        public static long ReadInt64(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(long), BitConverter.ToInt64);

        public static ulong ReadUInt64(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(ulong), BitConverter.ToUInt64);

        public static float ReadSingle(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(float), BitConverter.ToSingle);

        public static double ReadDouble(this byte[] bytes, Endianness byteOrder, int startIndex = 0) => Read(bytes, byteOrder, startIndex, sizeof(double), BitConverter.ToDouble);

        public static short ReadInt16FromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadInt16(bytes, NetworkOrder, startIndex);

        public static ushort ReadUInt16FromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadUInt16(bytes, NetworkOrder, startIndex);

        public static int ReadInt32FromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadInt32(bytes, NetworkOrder, startIndex);

        public static uint ReadUInt32FromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadUInt32(bytes, NetworkOrder, startIndex);

        public static long ReadInt64FromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadInt64(bytes, NetworkOrder, startIndex);

        public static ulong ReadUInt64FromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadUInt64(bytes, NetworkOrder, startIndex);

        public static float ReadSingleFromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadSingle(bytes, NetworkOrder, startIndex);

        public static double ReadDoubleFromNetworkOrder(this byte[] bytes, int startIndex = 0) => ReadDouble(bytes, NetworkOrder, startIndex);

        public static int WriteInt16(this byte[] bytes, short value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteUInt16(this byte[] bytes, ushort value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteInt32(this byte[] bytes, int value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteUInt32(this byte[] bytes, uint value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteInt64(this byte[] bytes, long value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteUInt64(this byte[] bytes, ulong value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteSingle(this byte[] bytes, float value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteDouble(this byte[] bytes, double value, Endianness byteOrder, int startIndex = 0)
        {
            var valueBytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) valueBytes.Reverse();
            return bytes.WriteValues(valueBytes, startIndex);
        }

        public static int WriteInt16AsNetworkOrder(this byte[] bytes, short value, int startIndex = 0) => WriteInt16(bytes, value, NetworkOrder, startIndex);

        public static int WriteUInt16AsNetworkOrder(this byte[] bytes, ushort value, int startIndex = 0) => WriteUInt16(bytes, value, NetworkOrder, startIndex);

        public static int WriteInt32AsNetworkOrder(this byte[] bytes, int value, int startIndex = 0) => WriteInt32(bytes, value, NetworkOrder, startIndex);

        public static int WriteUInt32AsNetworkOrder(this byte[] bytes, uint value, int startIndex = 0) => WriteUInt32(bytes, value, NetworkOrder, startIndex);

        public static int WriteInt64AsNetworkOrder(this byte[] bytes, long value, int startIndex = 0) => WriteInt64(bytes, value, NetworkOrder, startIndex);

        public static int WriteUInt64AsNetworkOrder(this byte[] bytes, ulong value, int startIndex = 0) => WriteUInt64(bytes, value, NetworkOrder, startIndex);

        public static int WriteSingleAsNetworkOrder(this byte[] bytes, float value, int startIndex = 0) => WriteSingle(bytes, value, NetworkOrder, startIndex);

        public static int WriteDoubleAsNetworkOrder(this byte[] bytes, double value, int startIndex = 0) => WriteDouble(bytes, value, NetworkOrder, startIndex);

        public static byte[] GetBytes(this short value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this ushort value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this int value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this uint value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this long value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this ulong value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this float value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytes(this double value, Endianness byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            if (SystemByteOrder != byteOrder) bytes.Reverse();
            return bytes;
        }

        public static byte[] GetBytesInNetworkOrder(this short value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this ushort value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this int value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this uint value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this long value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this ulong value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this float value) => GetBytes(value, NetworkOrder);

        public static byte[] GetBytesInNetworkOrder(this double value) => GetBytes(value, NetworkOrder);

        private static byte[] GetBuffer(int size)
        {
            ThreadLocal<byte[]> threadLocal;
            BufferLock.EnterUpgradeableReadLock();
            try
            {
                threadLocal = BufferThreadLocals[size - 1];
                if (threadLocal == null)
                {
                    BufferLock.EnterWriteLock();
                    BufferThreadLocals[size - 1] = threadLocal = new ThreadLocal<byte[]>(() => new byte[size]);
                }
            }
            finally
            {
                BufferLock.ExitUpgradeableReadLock();
            }
            return threadLocal.Value;
        }

        private static void ReversedCopy(IReadOnlyList<byte> src, int srcStartIndex, IList<byte> dst, int dstStartIndex, int count)
        {
            var srcIndex = srcStartIndex + count - 1;
            var dstIndex = dstStartIndex;
            for (var i = 0; i < count; i++)
            {
                dst[dstIndex] = src[srcIndex];
                srcIndex--;
                dstIndex++;
            }
        }

        private static T Read<T>(byte[] bytes, Endianness byteOrder, int startIndex, int size, Func<byte[], int, T> func) where T : struct
        {
            if (SystemByteOrder == byteOrder) return func(bytes, startIndex);
            var valueBytes = GetBuffer(size);
            ReversedCopy(bytes, startIndex, valueBytes, 0, size);
            return func(valueBytes, 0);
        }

    }
}
