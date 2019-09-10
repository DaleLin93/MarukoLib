using System.Windows;
using System.Windows.Forms;
using MarukoLib.Interop;

namespace MarukoLib.UI
{

    public static class CursorUtils
    {

        private sealed class CursorVisibility
        {

            private volatile bool _visible;

            public CursorVisibility(bool visible = true) => _visible = visible;

            // ReSharper disable once UnusedMethodReturnValue.Local
            public bool AsVisible(bool visible)
            {
                if (_visible == visible) return false;
                _visible = visible;
                SetCursorVisibility(visible);
                return true;
            }

        }

        public static void SetCursorVisibility(bool visible) => User32.ShowCursor(visible ? 1 : 0);

        public static void HideCursorOnFocus(this UIElement element)
        {
            var visibility = new CursorVisibility();
            element.GotFocus += (sender, args) => visibility.AsVisible(false);
            element.LostFocus += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorOnActive(this Window window)
        {
            var visibility = new CursorVisibility();
            window.Activated += (sender, args) => visibility.AsVisible(false);
            window.Deactivated += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorOnFocus(this Control control)
        {
            var visibility = new CursorVisibility();
            control.GotFocus += (sender, args) => visibility.AsVisible(false);
            control.LostFocus += (sender, args) => visibility.AsVisible(true);
            control.Disposed += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorInside(this UIElement element)
        {
            var visibility = new CursorVisibility();
            element.MouseEnter += (sender, args) => visibility.AsVisible(false);
            element.MouseLeave += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorInside(this Control control)
        {
            var visibility = new CursorVisibility();
            control.MouseEnter += (sender, args) => visibility.AsVisible(false);
            control.MouseLeave += (sender, args) => visibility.AsVisible(true);
            control.Disposed += (sender, args) => visibility.AsVisible(true);
        }

    }

}
