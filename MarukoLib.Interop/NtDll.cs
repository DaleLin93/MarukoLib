using System.Runtime.InteropServices;

namespace MarukoLib.Interop
{

    public static class NtDll
    {

        /// <summary>Copy a block of memory.</summary>
        /// <param name="dst">Destination pointer.</param>
        /// <param name="src">Source pointer.</param>
        /// <param name="count">Memory block's length to copy.</param>
        /// <returns>Return's the value of <b>dst</b> - pointer to destination.</returns>
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int memcpy(byte* dst, byte* src, int count);

    }

}
