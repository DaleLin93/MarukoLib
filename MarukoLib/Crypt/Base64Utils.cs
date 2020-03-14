using System;
using System.Text;
using JetBrains.Annotations;

namespace MarukoLib.Crypt
{

    public static class Base64Utils
    {

        public static string Encode([NotNull] Encoding encoding, [CanBeNull] string source) 
            => source == null ? null : Convert.ToBase64String(encoding.GetBytes(source));

        public static string Decode([NotNull] Encoding encoding, [CanBeNull] string encoded)
            => encoded == null ? null : encoding.GetString(Convert.FromBase64String(encoded));

        public static string EncodeBase64([CanBeNull] this string source, [CanBeNull] Encoding encoding = null) => Encode(encoding ?? Encoding.Default, source);

        public static string DecodeBase64([CanBeNull] this string result, [CanBeNull] Encoding encoding = null) => Decode(encoding ?? Encoding.Default, result);

    }

}
