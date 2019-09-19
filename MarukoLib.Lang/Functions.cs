using System;

namespace MarukoLib.Lang
{

    public static class Functions
    {

        public static Func<T> Constant<T>(T value) => () => value;

        public static T Identity<T>(T input) => input;

        public static bool NotNull<T>(T input) => input != null;

        public static bool IsNull<T>(T input) => input == null;

        public static TOut Compute<TIn, TOut>(this TIn input, Func<TIn, TOut> func) => func == null ? default : func.Invoke(input);

        public static sbyte Increment1(sbyte val) => (sbyte) (val + 1);

        public static byte Increment1(byte val) => (byte) (val + 1);

        public static short Increment1(short val) => (short) (val + 1);

        public static ushort Increment1(ushort val) => (ushort) (val + 1);

        public static int Increment1(int val) => val + 1;

        public static uint Increment1(uint val) => val + 1;

        public static long Increment1(long val) => val + 1;

        public static ulong Increment1(ulong val) => val + 1;


    }

}
