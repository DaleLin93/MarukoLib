using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using MarukoLib.Interop.Native;
using MarukoLib.Lang;

namespace MarukoLib.Interop
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class User32
    {

        public delegate IntPtr WndProc(IntPtr hWnd, int msg, int wParam, int lParam);

        public delegate bool WndEnumProc(IntPtr hWnd, int lParam);

        public delegate IntPtr HookProc(int nCode, int wParam, int lParam);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref Rect lpRect, int lParam);

        public const int WM_KEYDOWN = 0x100;

        public const int WM_KEYUP = 0x101;

        public const int WM_SYSKEYDOWN = 0x104;

        public const int WM_SYSKEYUP = 0x105;

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

        /// <summary>
        /// Monitor information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInfo
        {

            public const uint FlagPrimary = 1;
            
            public uint size;
            public Rect monitor;
            public Rect work;
            public uint flags;

            public bool IsPrimary => (flags & FlagPrimary) == FlagPrimary;

        }

        [StructLayout(LayoutKind.Sequential)]
        public class KeyboardHookStruct
        {
            public int vkCode;  //定一个虚拟键码。该代码必须有一个价值的范围1至254
            public int scanCode; // 指定的硬件扫描码的关键
            public int flags;  // 键标志
            public int time; // 指定的时间戳记的这个讯息
            public int dwExtraInfo; // 指定额外信息相关的信息
        }

        public static readonly WndProc DefaultWindowProc = DefWindowProc;

        [DllImport("user32.dll")]
        public extern static int PostMessage(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

      //  //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
      //  [DllImport("user32.dll")]
      //  private static extern IntPtr MonitorFromPoint([In]System.Drawing.Point pt, [In]uint dwFlags);

        /// <summary>
        /// Monitors from rect.
        /// </summary>
        /// <param name="rectPointer">The RECT pointer.</param>
        /// <param name="flags">The flags.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromRect([In] ref Rect rectPointer, uint flags);

        /// <summary>
        /// Enumerates through the display monitors.
        /// </summary>
        /// <param name="hdc">A handle to a display device context that defines the visible region of interest.</param>
        /// <param name="lprcClip">A pointer to a RECT structure that specifies a clipping rectangle.</param>
        /// <param name="lpfnEnum">A pointer to a MonitorEnumProc application-defined callback function.</param>
        /// <param name="dwData">Application-defined data that EnumDisplayMonitors passes directly to the MonitorEnumProc function.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, int dwData);

        /// <summary>
        /// Gets the monitor information.
        /// </summary>
        /// <param name="hMonitor">A handle to the display monitor of interest.</param>
        /// <param name="monitorInfo">A pointer to a MonitorInfo instance created by this method.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo monitorInfo);

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
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            int exStyle, string className, string windowName,
            int style, int x, int y, int width, int height,
            IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance,
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

        [DllImport("user32.dll")]
        public static extern int ShowCursor(int status);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        /// <summary>
        /// Installs an application-defined hook procedure into a hook chain. You would install a hook procedure to monitor the system for certain types of events. These events are associated either with a specific thread or with all threads in the same desktop as the calling thread.
        /// </summary>
        /// <param name="idHook">
        /// The type of hook procedure to be installed. This parameter can be one of the following values.
        /// WH_CALLWNDPROC: 4; Installs a hook procedure that monitors messages before the system sends them to the destination window procedure. For more information, see the CallWndProc hook procedure.
        /// WH_CALLWNDPROCRET: 12; Installs a hook procedure that monitors messages after they have been processed by the destination window procedure. For more information, see the CallWndRetProc hook procedure.
        /// WH_CBT: 5; Installs a hook procedure that receives notifications useful to a CBT application. For more information, see the CBTProc hook procedure.
        /// WH_DEBUG: 9; Installs a hook procedure useful for debugging other hook procedures. For more information, see the DebugProc hook procedure.
        /// WH_FOREGROUNDIDLE: 11; Installs a hook procedure that will be called when the application's foreground thread is about to become idle. This hook is useful for performing low priority tasks during idle time. For more information, see the ForegroundIdleProc hook procedure.
        /// WH_GETMESSAGE: 3; Installs a hook procedure that monitors messages posted to a message queue. For more information, see the GetMsgProc hook procedure.
        /// WH_JOURNALPLAYBACK: 1; Installs a hook procedure that posts messages previously recorded by a WH_JOURNALRECORD hook procedure. For more information, see the JournalPlaybackProc hook procedure.
        /// WH_JOURNALRECORD: 0; Installs a hook procedure that records input messages posted to the system message queue. This hook is useful for recording macros. For more information, see the JournalRecordProc hook procedure.
        /// WH_KEYBOARD: 2; Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc hook procedure.
        /// WH_KEYBOARD_LL: 13; Installs a hook procedure that monitors low-level keyboard input events. For more information, see the LowLevelKeyboardProc hook procedure.
        /// WH_MOUSE: 7; Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook procedure.
        /// WH_MOUSE_LL: 14; Installs a hook procedure that monitors low-level mouse input events. For more information, see the LowLevelMouseProc hook procedure.
        /// WH_MSGFILTER: -1; Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box, message box, menu, or scroll bar. For more information, see the MessageProc hook procedure.
        /// WH_SHELL: 10; Installs a hook procedure that receives notifications useful to shell applications. For more information, see the ShellProc hook procedure.
        /// WH_SYSMSGFILTER: 6; Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box, message box, menu, or scroll bar. The hook procedure monitors these messages for all applications in the same desktop as the calling thread. For more information, see the SysMsgProc hook procedure.
        /// </param>
        /// <param name="lpfn">A pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a thread created by a different process, the lpfn parameter must point to a hook procedure in a DLL. Otherwise, lpfn can point to a hook procedure in the code associated with the current process.</param>
        /// <param name="hmod">A handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is within the code associated with the current process.</param>
        /// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated. For desktop apps, if this parameter is zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread. For Windows Store apps, see the Remarks section.</param>
        /// <returns>
        /// If the function succeeds, the return value is the handle to the hook procedure.
        /// If the function fails, the return value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);

        /// <summary>
        /// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
        /// </summary>
        /// <param name="hhk">A handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx.</param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information.
        /// </summary>
        /// <param name="hhk">This parameter is ignored.</param>
        /// <param name="nCode">The hook code passed to the current hook procedure. The next hook procedure uses this code to determine how to process the hook information.</param>
        /// <param name="wParam">The wParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <param name="lParam">The lParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <returns>This value is returned by the next hook procedure in the chain. The current hook procedure must also return this value. The meaning of the return value depends on the hook type. For more information, see the descriptions of the individual hook procedures.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam, int lParam);

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

        public static ICollection<IntPtr> EnumMonitors(IntPtr? hdc = null, Rect? clip = null)
        {
            var list = new LinkedList<IntPtr>();
            using (var clipPtr = clip?.AllocUnmanaged(false) ?? new Disposable<IntPtr>.NoAction(IntPtr.Zero))
            {
                EnumDisplayMonitors(hdc ?? IntPtr.Zero, clipPtr.Value, (IntPtr hMonitor, IntPtr hDc, ref Rect rect, int p) =>
                {
                    list.AddLast(hMonitor);
                    return true;
                }, 0);
            }
            return list;
        }

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

        public static MonitorInfo GetMonitorInfo(IntPtr hMonitor)
        {
            var monitorInfo = new MonitorInfo();
            GetMonitorInfo(hMonitor, ref monitorInfo);
            return monitorInfo;
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
