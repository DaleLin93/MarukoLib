using System;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.Win32;

namespace MarukoLib.Interop
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public abstract class Win32Control : HwndHost, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public object Content => this;

        public object DoubleClickControl;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private const string WindowClass = "WpfDirectXControl";

        protected const int WHEEL_DELTA = 120;

        protected IntPtr Hwnd { get; private set; }

        protected bool HwndInitialized { get; private set; }

        protected abstract void Initialize();

        protected abstract void UnInitialize();

        protected abstract void Resized();

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var wndClass = new User32.WndClassEx();
            wndClass.cbSize = (uint) Marshal.SizeOf(wndClass);
            wndClass.hInstance = Kernel32.GetModuleHandle(null);
            wndClass.lpfnWndProc = User32.DefaultWindowProc;
            wndClass.lpszClassName = WindowClass;
            wndClass.hCursor = User32.LoadCursor(IntPtr.Zero, (int) Interop.Cursor.Arrow);
            wndClass.style = 0x0008;

            User32.RegisterClassEx(ref wndClass);

            Hwnd = User32.CreateWindowEx(
                0, WindowClass, "", (int) (WindowStyle.Child | WindowStyle.Visible),
                0, 0, (int) ActualWidth, (int) ActualHeight, hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, 0);

            Initialize();
            HwndInitialized = true;
            return new HandleRef(this, Hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            UnInitialize();
            User32.DestroyWindow(hwnd.Handle);
            Hwnd = IntPtr.Zero;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            UpdateWindowPos();
            base.OnRenderSizeChanged(sizeInfo);
            if (HwndInitialized) Resized();
        }

        protected override IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WindowMessage) msg)
            {
                case WindowMessage.LeftButtonDown:
                    User32.SetFocus(hWnd);
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.MouseDownEvent,
                        Source = this,
                    });
                    break;
                case WindowMessage.LeftButtonUp:
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.MouseUpEvent,
                        Source = this,
                    });
                    break;
                case WindowMessage.RightButtonDown:
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
                    {
                        RoutedEvent = Mouse.MouseDownEvent,
                        Source = this,
                    });
                    break;
                case WindowMessage.RightButtonUp:
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
                    {
                        RoutedEvent = Mouse.MouseUpEvent,
                        Source = this,
                    });
                    break;
                case WindowMessage.MouseMove:
                {
                    var x = LOWORD(lParam.ToInt32());
                    var y = HIWORD(lParam.ToInt32());
                    var p = TransformToAncestor((Visual) Parent).Transform(new Point(0, 0));

                    const RawMouseActions actions = RawMouseActions.Activate | RawMouseActions.AbsoluteMove;
                    RaisePreviewMouseMoveEvent(
                        InputMode.Foreground,
                        actions,
                        PresentationSource.FromVisual(this),
                        Mouse.MouseMoveEvent,
                        Environment.TickCount, (int) (x + p.X), (int) (y + p.Y), 0);

                    RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
                    {
                        RoutedEvent = Mouse.MouseMoveEvent,
                        Source = this
                    });
                }
                    break;
                case WindowMessage.MouseWheel:
                    short delta;
                    switch (IntPtr.Size)
                    {
                        case 8:
                            delta = (short) (wParam.ToInt64() >> 16);
                            break;
                        case 4:
                            delta = (short) (wParam.ToInt32() >> 16);
                            break;
                        default:
                            delta = 0;
                            break;
                    }

                    RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, delta)
                    {
                        RoutedEvent = Mouse.MouseWheelEvent,
                        Source = this
                    });
                    break;
                case WindowMessage.LeftButtonDoubleClick:
                {
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Control.MouseDoubleClickEvent,
                        Source = DoubleClickControl,
                    });

                }

                    break;
                case WindowMessage.RightButtonDoubleClick:
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
                    {
                        RoutedEvent = Control.MouseDoubleClickEvent,
                        Source = this,
                    });
                    break;
                case WindowMessage.MiddleButtonDoubleClick:
                    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Middle)
                    {
                        RoutedEvent = Control.MouseDoubleClickEvent,
                        Source = this,
                    });
                    break;
                default:
                    return base.WndProc(hWnd, msg, wParam, lParam, ref handled);
            }

            //handled = true;
            return IntPtr.Zero;
        }

        /// <summary>
        ///     Constructs ad instance of the RawMouseInputReport class.
        /// </summary>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="inputSource">
        ///     The PresentationSource over which the mouse is moved.
        /// </param>
        /// <param name="actions">
        ///     The set of actions being reported.
        /// </param>
        /// <param name="x">
        ///     If horizontal position being reported.
        /// </param>
        /// <param name="y">
        ///     If vertical position being reported.
        /// </param>
        /// <param name="wheel">
        ///     If wheel delta being reported.
        /// </param>
        /// <param name="extraInformation">
        ///     Any extra information being provided along with the input.
        /// </param>
        /// <SecurityNote>
        ///     Critical:This handles critical data in the form of PresentationSource and ExtraInformation 
        ///     TreatAsSafe:There are demands on the  critical data(PresentationSource/ExtraInformation)
        /// </SecurityNote>
        private static void RaisePreviewMouseMoveEvent(InputMode mode, RawMouseActions actions, PresentationSource inputSource, RoutedEvent @event, int timestamp, int pointX, int pointY, int wheel)
        {
            var targetAssembly = Assembly.GetAssembly(typeof(InputEventArgs));
            var mouseInputReportType = targetAssembly.GetType("System.Windows.Input.RawMouseInputReport");

            // RawMouseInputReport mouseInputReport = new RawMouseInputReport(...);
            var mouseInputReport = mouseInputReportType.GetConstructors()[0].Invoke(new object[]
            {
                mode,
                timestamp,
                inputSource,
                actions,
                pointX,
                pointY,
                wheel,
                IntPtr.Zero
            });

            // mouseInputReport._isSynchronize = true;
            mouseInputReportType
                .GetField("_isSynchronize", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(mouseInputReport, true);

            // InputReportEventArgs inputReportEventArgs = new InputReportEventArgs(...);
            var inputReportEventArgs = (InputEventArgs) targetAssembly
                .GetType("System.Windows.Input.InputReportEventArgs")
                .GetConstructors()[0]
                .Invoke(new Object[] {Mouse.PrimaryDevice, mouseInputReport});

            inputReportEventArgs.RoutedEvent = (RoutedEvent) typeof(InputManager)
                .GetField("PreviewInputReportEvent", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .GetValue(null);

            //this.RaiseEvent((InputEventArgs)inputReportEventArgs);
            InputManager.Current.ProcessInput(inputReportEventArgs);
        }

        public static int LOWORD(int l) => l & 0xffff;

        public static int HIWORD(int l) => (l >> 16) & 0xffff;

        public static int LOBYTE(int l) => l & 0xff;

        public static int HIBYTE(int l) => (l >> 8) & 0xff;

        public static int GET_WHEEL_DELTA_WPARAM(int wParam) => HIWORD(wParam);

    }

    public static class RegistryHelper
    {
        public static string GetUserDirectory()
        {
            try
            {
                var localKey64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                var rkApp = localKey64.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders", false);
                return rkApp.GetValue("Personal") as string;
            }
            catch
            {
                return null;
            }
        }

        public static string GetUserPictureDirectory()
        {
            try
            {
                var localKey64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                var rkApp = localKey64.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders", false);
                return rkApp.GetValue("My Pictures") as string;
            }
            catch
            {
                return null;
            }
        }

        public static string GetUserAppDataDirectory()
        {
            try
            {
                var localKey64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                var rkApp = localKey64.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders", false);
                return rkApp.GetValue("Local AppData") as string;
            }
            catch
            {
                return null;
            }
        }
    }

    [Flags]
    public enum RawMouseActions
    {
        None = 0,
        AttributesChanged = 1,
        Activate = 2,
        Deactivate = 4,
        RelativeMove = 8,
        AbsoluteMove = 16,
        VirtualDesktopMove = 32,
        Button1Press = 64,
        Button1Release = 128,
        Button2Press = 256,
        Button2Release = 512,
        Button3Press = 1024,
        Button3Release = 2048,
        Button4Press = 4096,
        Button4Release = 8192,
        Button5Press = 16384,
        Button5Release = 32768,
        VerticalWheelRotate = 65536,
        HorizontalWheelRotate = 131072,
        QueryCursor = 262144,
        CancelCapture = 524288
    }

    [Flags]
    public enum WindowStyle
    {

        Overlapped = 0x00000000,
        PopUp = unchecked((int) 0x80000000),
        Child = 0x40000000,
        Minimize = 0x20000000,
        Visible = 0x10000000,
        Disabled = 0x08000000,
        ClipSiblings = 0x04000000,
        ClipChildren = 0x02000000,
        Maxmize = 0x01000000,
        Caption = Border | DlgFrame, /* WS_BORDER | WS_DLGFRAME  */
        Border = 0x00800000,
        DlgFrame = 0x00400000,
        VScroll = 0x00200000,
        HScroll = 0x00100000,
        SysMenu = 0x00080000,
        ThickFrame = 0x00040000,
        Group = 0x00020000,
        TabStop = 0x00010000,
        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000,
        Tiled = Overlapped,
        Iconic = Minimize,
        SizeBox = ThickFrame,
        OverlappedWindow = Overlapped | Caption | SysMenu | ThickFrame | MinimizeBox | MaximizeBox,
        TiledWindow = OverlappedWindow,
        PopUpWindow = PopUp | Border | SysMenu,
        ChildWindow = Child
    }

    [Flags]
    public enum WindowMessage
    {
        NonclientAreaLeftButtonDoubleClick = 0x00A3,
        MouseFirst = 0x0200,
        MouseMove = 0x0200,
        LeftButtonDown = 0x0201,
        LeftButtonUp = 0x0202,
        LeftButtonDoubleClick = 0x0203,
        RightButtonDown = 0x0204,
        RightButtonUp = 0x0205,
        RightButtonDoubleClick = 0x0206,
        MiddleButtonDown = 0x0207,
        MiddleButtonUp = 0x0208,
        MiddleButtonDoubleClick = 0x0209,
        MouseWheel = 0x020A,
        XButtonDown = 0x020B,
        XButtonUp = 0x020C,
        XButtonDoubleClick = 0x020D,
        MouseHorizontalWheel = 0x020E,
        MouseLast = 0x020E,
        DeviceChange = 0x0219,
    }

    [Flags]
    public enum MaskKey
    {
        Control = 0x0008,
        LeftButton = 0x0001,
        MiddleButton = 0x0010,
        RightButton = 0x0002,
        Shift = 0x0004,
        XButton1 = 0x0020,
        XButton2 = 0x0040,
    }

    public enum Cursor
    {
        AppStarting = 32650,
        Arrow = 32512,
        Cross = 32515,
        Hand = 32649,
        Help = 32651,
        I_beam = 32513,

        //Icon = 32641, //Obsolete for applications marked version 4.0 or later.
        No = 32648,

        //Size = 32640, //Obsolete for applications marked version 4.0 or later. Use SizeAll
        SizeAll = 32646,
        SizeNortheastAndSouthwest = 32643,
        SizeNorthSouth = 32645,
        SizeNorthwestAndSoutheast = 32642,
        SizeWestEast = 32644,
        UpArrow = 32516,
        Wait = 32514,
    }
}