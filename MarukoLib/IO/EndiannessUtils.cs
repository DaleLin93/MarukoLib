using System;
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

        public static byte InvertBits(this byte b) => (byte)~b;

        public static byte ReverseBits(this byte b)
        {
            b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
            b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
            b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
            return b;
        }

        public static short ReadInt16(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToInt16(bytes, startIndex);
            var valueBytes = new[] { bytes[startIndex + 1], bytes[startIndex + 0] };
            return BitConverter.ToInt16(valueBytes, 0);
        }

        public static ushort ReadUInt16(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToUInt16(bytes, startIndex);
            var valueBytes = new[] { bytes[startIndex + 1], bytes[startIndex + 0] };
            return BitConverter.ToUInt16(valueBytes, 0);
        }

        public static int ReadInt32(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToInt32(bytes, startIndex);
            var valueBytes = new[] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex + 0] };
            return BitConverter.ToInt32(valueBytes, 0);
        }

        public static uint ReadUInt32(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToUInt32(bytes, startIndex);
            var valueBytes = new[] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex + 0] };
            return BitConverter.ToUInt32(valueBytes, 0);
        }

        public static long ReadInt64(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToInt64(bytes, startIndex);
            var valueBytes = new[]
            {
                bytes[startIndex + 7], bytes[startIndex + 6], bytes[startIndex + 5], bytes[startIndex + 4],
                bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex + 0],
            };
            return BitConverter.ToInt64(valueBytes, 0);
        }

        public static ulong ReadUInt64(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToUInt64(bytes, startIndex);
            var valueBytes = new[]
            {
                bytes[startIndex + 7], bytes[startIndex + 6], bytes[startIndex + 5], bytes[startIndex + 4],
                bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex + 0],
            };
            return BitConverter.ToUInt64(valueBytes, 0);
        }

        public static float ReadSingle(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToSingle(bytes, startIndex);
            var valueBytes = new[] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex + 0] };
            return BitConverter.ToSingle(valueBytes, 0);
        }

        public static double ReadDouble(this byte[] bytes, Endianness byteOrder, int startIndex = 0)
        {
            if (SystemByteOrder == byteOrder) return BitConverter.ToDouble(bytes, startIndex);
            var valueBytes = new[]
            {
                bytes[startIndex + 7], bytes[startIndex + 6], bytes[startIndex + 5], bytes[startIndex + 4],
                bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex + 0],
            };
            return BitConverter.ToDouble(valueBytes, 0);
        }

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

    }
}
