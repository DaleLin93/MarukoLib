using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace MarukoLib.Interop
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class Kernel32
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HDR
        {
            public uint dbch_size;
            public uint dbch_devicetype;
            public uint dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_OEM
        {
            public uint dbco_size;
            public uint dbco_devicetype;
            public uint dbco_reserved;
            public uint dbco_identifier;
            public uint dbco_suppfunc;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_NET
        {
            public uint dbcn_size;
            public uint dbcn_devicetype;
            public uint dbcn_reserved;
            public uint dbcn_resource;
            public uint dbcn_flags;
        };

        public struct DEV_BROADCAST_VOLUME
        {
            public uint dbcv_size;
            public uint dbcv_devicetype;
            public uint dbcv_reserved;
            public uint dbcv_unitmask;
            public ushort dbcv_flags;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_PORT
        {
            public int dbcp_size;
            public int dbcp_devicetype;
            public int dbcp_reserved;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcp_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class OVERLAPPED
        {
            public int Internal;
            public int InternalHigh;
            public int Offset;
            public int OffSetHigh;
            public int hEvent;
        }

        [Flags]
        public enum DeviceBroadcast
        {
            Arrival = 0x8000, // system detected a new device
            QueryRemove = 0x8001, // wants to remove, may fail
            QueryRemoveFailed = 0x8002, // removal aborted
            RemovePending = 0x8003, // about to remove, still avail.
            RemoveComplete = 0x8004, // device is gone
            TypeSpecific = 0x8005, // type specific event
        }

        [Flags]
        public enum DeviceBroadcastType
        {
            Oem = 0x00000000, //OEM- or IHV-defined device type
            Volume = 0x00000002, //Logical volume
            Port = 0x00000003, //Port device (serial or parallel)
            DeviceInterface = 0x00000005, //Class of devices
            Handle = 0x00000006, //File system handle
        }

        [Flags]
        public enum DeviceNotifyHandle
        {
            Window = 0,
            Service = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OsVersionInfoExW
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
            [MarshalAs(UnmanagedType.LPTStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToWrite,
            ref int lpNumberOfBytesWriten, ref OVERLAPPED lpOverLapped);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToRead,
            ref int lpNumberOfBytesWriten, ref OVERLAPPED lpOverLapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [Obsolete("Use Marshal.GetLastWin32Error", true)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetLastError();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string module);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern ulong VerSetConditionMask(ulong conditionMask, uint typeMask, byte condition);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool VerifyVersionInfoW([In] ref OsVersionInfoExW lpVersionInformation, uint dwTypeMask, ulong dwlConditionMask);

    }

}
