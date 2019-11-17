using System;
using System.Runtime.InteropServices;

namespace MarukoLib.Interop
{

    public static class ShCore
    {

        public enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
            Default = 3
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("shcore.dll")]
        public static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]DpiType dpiType, [Out]out uint dpiX, [Out]out uint dpiY);

    }

}
