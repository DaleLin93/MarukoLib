using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using MarukoLib.Interop.Native;

namespace MarukoLib.Interop
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class User32
    {

        public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public delegate bool WndEnumProc(IntPtr hwnd, int lparm);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left, Top, Right, Bottom;

            public override string ToString() => $"{nameof(Left)}: {Left}, {nameof(Top)}: {Top}, {nameof(Right)}: {Right}, {nameof(Bottom)}: {Bottom}";
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WndClassEx
        {
            public uint cbSize;
            public uint style;
            [MarshalAs(UnmanagedType.FunctionPtr)] public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        public static readonly WndProc DefaultWindowProc = DefWindowProc;

        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(WndEnumProc lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);

        /// <summary>
        /// Changes the size, position, and Z order of a child, pop-up, or top-level window. These windows are ordered according to their appearance on the screen. The topmost window receives the highest rank and is the first window in the Z order.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="hWndInsertAfter">
        /// A handle to the window to precede the positioned window in the Z order. This parameter must be a window handle or one of the following values.
        ///  - HWND_BOTTOM: 1; Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
        ///  - HWND_NOTOPMOST: -2; Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
        ///  - HWND_TOP: 0; Places the window at the top of the Z order.
        ///  - HWND_TOPMOST: -1; Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
        /// </param>
        /// <param name="X">The new position of the left side of the window, in client coordinates.</param>
        /// <param name="Y">The new position of the top of the window, in client coordinates.</param>
        /// <param name="cx">The new width of the window, in pixels.</param>
        /// <param name="cy">The new height of the window, in pixels.</param>
        /// <param name="uFlags">
        /// The window sizing and positioning flags. This parameter can be a combination of the following values.
        /// SWP_ASYNCWINDOWPOS: 0x4000; If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
        /// SWP_DEFERERASE: 0x2000; Prevents generation of the WM_SYNCPAINT message.
        /// SWP_DRAWFRAME: 0x0020; Draws a frame (defined in the window's class description) around the window.
        /// SWP_FRAMECHANGED: 0x0020; Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
        /// SWP_HIDEWINDOW: 0x0080; Hides the window.
        /// SWP_NOACTIVATE: 0x0010; Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
        /// SWP_NOCOPYBITS: 0x0100; 	Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
        /// SWP_NOMOVE: 0x0002; Retains the current position (ignores X and Y parameters).
        /// SWP_NOOWNERZORDER: 0x0200; Does not change the owner window's position in the Z order.
        /// SWP_NOREDRAW: 0x0008; Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
        /// SWP_NOREPOSITION: 0x0200; Same as the SWP_NOOWNERZORDER flag.
        /// SWP_NOSENDCHANGING: 0x0400; Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
        /// SWP_NOSIZE: 0x0001; Retains the current size (ignores the cx and cy parameters).
        /// SWP_NOZORDER: 0x0004; Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// SWP_SHOWWINDOW: 0x0040; Displays the window.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpText, int nCount);

        /// <summary>
        /// Win32 (預設)控制項訊息回應方法
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            int exStyle,
            string className,
            string windowName,
            int style,
            int x, int y,
            int width, int height,
            IntPtr hwndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.U2)]
        public static extern short RegisterClassEx([In] ref WndClassEx lpwcx);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterDeviceNotification(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll", EntryPoint = "ShowCursor", CharSet = CharSet.Auto)]
        public static extern int ShowCursor(int status);

        public static bool RegisterDeviceNotification(Window window, Guid guid)
        {
            var hWnd = (new WindowInteropHelper(window)).Handle;

            var dbi = new Kernel32.DEV_BROADCAST_DEVICEINTERFACE();

            var size = Marshal.SizeOf(dbi);
            dbi.dbcc_size = size;
            dbi.dbcc_devicetype = (int)Kernel32.DeviceBroadcastType.DeviceInterface;

            dbi.dbcc_reserved = 0;

            //USB
            dbi.dbcc_classguid = guid;

            dbi.dbcc_name = null;

            var buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(dbi, buffer, true);
            var h = RegisterDeviceNotification(hWnd, buffer, (uint)Kernel32.DeviceNotifyHandle.Window);
            return h != IntPtr.Zero;
        }

        public static bool UnregisterDeviceNotification(Window window) => UnregisterDeviceNotification(new WindowInteropHelper(window).Handle);

        public static T UnmanagedToManaged<T>(IntPtr ptr) => (T)Marshal.PtrToStructure(ptr, typeof(T));

        public static ICollection<IntPtr> EnumWindows()
        {
            var list = new LinkedList<IntPtr>();
            EnumWindows((hWnd, p) =>
            {
                list.AddLast(hWnd);
                return true;
            }, 0);
            return list;
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            var len = GetWindowTextLength(hWnd);
            var stringBuilder = new StringBuilder(len + 1);
            GetWindowText(hWnd, stringBuilder, len + 1);
            return stringBuilder.ToString();
        }

        public static Rect GetWindowRect(IntPtr hWnd)
        {
            var rect = new Rect();
            GetWindowRect(hWnd, ref rect);
            return rect;
        }

    }

}
