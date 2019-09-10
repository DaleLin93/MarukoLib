using System;
using System.IO.Ports;
using MarukoLib.Lang;

namespace MarukoLib.IO
{
    public static class SerialPortUtils
    {

        public static void ReadFully(this SerialPort port, byte[] buf, long timeoutMillis = 0) => ReadFully(port, buf, 0, buf.Length, timeoutMillis);

        public static void ReadFully(this SerialPort port, byte[] buf, int offset, int size, long timeoutMillis = 0)
        {
            var start = DateTimeUtils.CurrentTimeMillis;
            var remaining = size;
            do
            {
                if (port.BytesToRead > 0)
                    remaining -= port.Read(buf, offset + (size - remaining), remaining);
                if (timeoutMillis > 0 && start + timeoutMillis < DateTimeUtils.CurrentTimeMillis)
                    throw new TimeoutException();
            } while (remaining > 0);
        }

    }
}
