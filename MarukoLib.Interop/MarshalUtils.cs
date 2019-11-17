using System;
using System.Runtime.InteropServices;
using MarukoLib.Lang;

namespace MarukoLib.Interop
{
    public static class MarshalUtils
    {
        /// <param name="structure"></param>
        /// <param name="destroy"> to call the <see cref="M:System.Runtime.InteropServices.Marshal.DestroyStructure(System.IntPtr,System.Type)" /> method before release the unmanaged memory. Note that passing <see langword="false" /> when the memory block already contains data can lead to a memory leak.</param>
        public static Disposable<IntPtr> AllocUnmanaged<T>(this T structure, bool destroy) where T : struct
        {
            var structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            Marshal.StructureToPtr(structure, structPtr, false);
            Action<IntPtr> freeAction;
            if (destroy)
                freeAction = ptr =>
                {
                    Marshal.DestroyStructure(ptr, typeof(T));
                    Marshal.FreeHGlobal(ptr);
                };
            else
                freeAction = Marshal.FreeHGlobal;
            return new Disposable<IntPtr>.Delegated(structPtr, freeAction);
        }

    }
}
