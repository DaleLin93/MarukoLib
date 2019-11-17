using System;
using System.Diagnostics.CodeAnalysis;
using MarukoLib.Interop;

namespace MarukoLib.UI
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public static class GraphicsUtils
    {

        public static double Scale
        {
            get
            {
                var desktop = DesktopHdc;
                return GetScreenScale(desktop) * GetDpiScale(desktop);
            }
        }

        public static IntPtr DesktopHdc => User32.GetDC(IntPtr.Zero);

        public static double GetScreenScale(IntPtr? desktop)
        {
            var hdc = desktop ?? DesktopHdc;
            var logicalScreenHeight = Gdi32.GetDeviceCaps(hdc, (int) Gdi32.DeviceCap.VERTRES);
            var physicalScreenHeight = Gdi32.GetDeviceCaps(hdc, (int) Gdi32.DeviceCap.DESKTOPVERTRES);
            var screenScalingFactor = physicalScreenHeight / (double) logicalScreenHeight;
            return screenScalingFactor;
        }

        public static double GetDpiScale(IntPtr? desktop)
        {
            var hdc = desktop ?? DesktopHdc;
            var logpixelsy = Gdi32.GetDeviceCaps(hdc, (int) Gdi32.DeviceCap.LOGPIXELSY);
            var dpiScalingFactor = logpixelsy / 96.0D;
            return dpiScalingFactor;
        }

    }

}