using System;
using System.IO;
using System.Net.Sockets;
using MarukoLib.Lang;

namespace MarukoLib.IO
{
    public static class StreamUtils
    {

        public static void WriteAscii(this Stream stream, object val)
        {
            var strVal = val.ToString();
            foreach (var c in strVal)
                stream.WriteByte((byte)c);
        }

        public static void WriteFully(this Stream stream, byte[] buf, bool flush = true)
        {
            stream.Write(buf, 0, buf.Length);
            if (flush) stream.Flush();
        }

        public static void SkipBytes(this Stream stream, uint byteCount)
        {
            if (stream.CanSeek)
                stream.Seek(byteCount, SeekOrigin.Current);
            else
            {
                var remaining = byteCount;
                var b = -1;
                while (remaining > 0)
                {
                    try
                    {
                        b = stream.ReadByte();
                    }
                    catch (EndOfStreamException) { throw new EndOfStreamException(); }
                    catch (IOException e)
                    {
                        if (e.InnerException is SocketException socketEx)
                            throw new IOException($"Socket error occurred, error code: {socketEx.ErrorCode}", socketEx);
                    }
                    if (b == -1) throw new EndOfStreamException();
                    remaining--;
                } 
            }
        }

        public static void ReadFully(this Stream stream, byte[] buf, long timeoutMillis = 0) => ReadFully(stream, buf, 0, buf.Length, timeoutMillis);

        public static void ReadFully(this Stream stream, byte[] buf, int offset, int size, long timeoutMillis = 0, Predicate<SocketException> exceptionHandler = null)
        {
            var start = DateTimeUtils.CurrentTimeMillis;
            var remaining = size;
            var readout = 0;
            do
            {
                try
                {
                    readout = stream.Read(buf, offset + size - remaining, remaining);
                }
                catch (EndOfStreamException) { throw new EndOfStreamException(); }
                catch (IOException e)
                {
                    if (e.InnerException is SocketException socketEx)
                    {
                        if (exceptionHandler?.Invoke(socketEx) ?? false) goto check_time;
                        throw new IOException($"Socket error occurred, error code: {socketEx.ErrorCode}", socketEx);
                    }
                }
                if (readout == 0) throw new EndOfStreamException();
                remaining -= readout;
                check_time:
                if (timeoutMillis > 0 && start + timeoutMillis < DateTimeUtils.CurrentTimeMillis)
                    throw new TimeoutException($"Read timeout, {size - remaining}/{size} received");
            } while (remaining > 0);
        }

    }
}
