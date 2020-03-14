using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MarukoLib.IO
{
    public static class GZipUtils
    {

        public static string DecompressBase64(string input)
        {
            var compressed = Convert.FromBase64String(input);
            var decompressed = DecompressWithLength(compressed);
            return Encoding.UTF8.GetString(decompressed);
        }

        public static string CompressBase64(string input)
        {
            var encoded = Encoding.UTF8.GetBytes(input);
            var compressed = CompressWithLength(encoded);
            return Convert.ToBase64String(compressed);
        }

        public static byte[] DecompressWithLength(byte[] input)
        {
            using (var source = new MemoryStream(input))
            {
                var lengthBytes = new byte[4];
                source.Read(lengthBytes, 0, 4);
                var length = BitConverter.ToInt32(lengthBytes, 0);
                using (var decompressionStream = new GZipStream(source, CompressionMode.Decompress))
                {
                    var result = new byte[length];
                    decompressionStream.Read(result, 0, length);
                    return result;
                }
            }
        }

        public static byte[] CompressWithLength(byte[] input)
        {
            using (var result = new MemoryStream())
            {
                var lengthBytes = BitConverter.GetBytes(input.Length);
                result.Write(lengthBytes, 0, 4);
                using (var compressionStream = new GZipStream(result, CompressionMode.Compress))
                {
                    compressionStream.Write(input, 0, input.Length);
                    compressionStream.Flush();
                }
                return result.ToArray();
            }
        }

        public static byte[] Decompress(byte[] input)
        {
            using (var src = new MemoryStream(input))
            using (var gzip = new GZipStream(src, CompressionMode.Decompress))
            using (var dst = new MemoryStream())
            {
                var buffer = new byte[1024];
                int read;
                while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0) dst.Write(buffer, 0, read);
                return dst.ToArray();
            }
        }

        public static byte[] Compress(byte[] input)
        {
            using (var dst = new MemoryStream())
            {
                using (var gzip = new GZipStream(dst, CompressionMode.Compress))
                {
                    gzip.Write(input, 0, input.Length);
                    gzip.Flush();
                }
                return dst.ToArray();
            }
        }

    }
}
