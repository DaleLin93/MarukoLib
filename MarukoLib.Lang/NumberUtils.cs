namespace MarukoLib.Lang
{

    public static class NumberUtils
    {

        public static int ParseByte(string str, byte defaultVal) => byte.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static int ParseSByte(string str, sbyte defaultVal) => sbyte.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static short ParseShort(string str, short defaultVal) => short.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static ushort ParseUShort(string str, ushort defaultVal) => ushort.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static int ParseInt(string str, int defaultVal) => int.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static uint ParseUInt(string str, uint defaultVal) => uint.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static int? ParseInt(string str, int? defaultVal = null) => int.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static long ParseLong(string str, long defaultVal) => long.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static ulong ParseULong(string str, ulong defaultVal) => ulong.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static long? ParseLong(string str, long? defaultVal = null) => long.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static float ParseFloat(string str, float defaultVal) => float.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static float? ParseFloat(string str, float? defaultVal = null) => float.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static double ParseDouble(string str, double defaultVal) => double.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static double? ParseDouble(string str, double? defaultVal = null) => double.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static decimal ParseDecimal(string str, decimal defaultVal) => decimal.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static decimal? ParseDecimal(string str, decimal? defaultVal = null) => decimal.TryParse(str, out var parsed) ? parsed : defaultVal;

        public static bool IsDigit(char c) => c >= '0' && c <= '9';

        public static bool IsDigit(string str)
        {
            if(str.Length == 0)
                return false;
            foreach (var ch in str)
                if (!IsDigit(ch))
                    return false;
            return true;
        }

    }
}
