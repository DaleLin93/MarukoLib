using System;
using System.Text;
using MarukoLib.Logging;
using MarukoLib.Threading;

namespace MarukoLib.Crypt
{
    public static class Base64Utils
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ThreadUtils));

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="encoding">加密采用的编码方式</param>
        /// <param name="source">待加密的明文</param>
        /// <returns></returns>
        public static string EncodeBase64(Encoding encoding, string source)
        {
            var bytes = encoding.GetBytes(source);
            try
            {
                return Convert.ToBase64String(bytes);
            }
            catch (Exception e)
            {
                Logger.Error("EncodeBase64", e, "encoding", encoding);
                return source;
            }
        }

        /// <summary>
        /// Base64加密，采用utf8编码方式加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <param name="encoding">加密采用的编码方式</param>
        /// <returns>加密后的字符串</returns>
        public static string EncodeBase64(this string source, Encoding encoding = null) => EncodeBase64(encoding ?? Encoding.Default, source);

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="encoding">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string DecodeBase64(Encoding encoding, string result)
        {
            string decode;
            var bytes = Convert.FromBase64String(result);
            try
            {
                decode = encoding.GetString(bytes);
            }
            catch (Exception e)
            {
                Logger.Error("DecodeBase64", e, "encoding", encoding);
                decode = result;
            }
            return decode;
        }

        /// <summary>
        /// Base64解密，采用utf8编码方式解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <param name="encoding">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <returns>解密后的字符串</returns>
        public static string DecodeBase64(this string result, Encoding encoding = null) => DecodeBase64(encoding ?? Encoding.Default, result);

    }

}
