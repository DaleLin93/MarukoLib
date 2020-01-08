namespace MarukoLib.Lang
{

    public static class Predicates
    {

        public static bool IsNull<T>(T value) => value == null;

        public static bool NotNull<T>(T value) => value != null;

        public static bool IsEmpty(string str) => str?.IsEmpty() ?? true;

        public static bool NotEmpty(string str) => str?.IsNotEmpty() ?? false;

        public static bool IsBlank(string str) => str?.IsBlank() ?? true;

        public static bool NotBlank(string str) => str?.IsNotBlank() ?? false;

        public static bool Positive(byte value) => value > 0;

        public static bool Positive(sbyte value) => value > 0;

        public static bool Positive(short value) => value > 0;

        public static bool Positive(ushort value) => value > 0;

        public static bool Positive(int value) => value > 0;

        public static bool Positive(uint value) => value > 0;

        public static bool Positive(long value) => value > 0;

        public static bool Positive(ulong value) => value > 0;

        public static bool Positive(float value) => value > 0;

        public static bool Positive(double value) => value > 0;

        public static bool Positive(decimal value) => value > 0;

        public static bool Nonnegative(float value) => value >= 0;

        public static bool Nonnegative(double value) => value >= 0;

        public static bool Nonnegative(decimal value) => value >= 0;

    }

}