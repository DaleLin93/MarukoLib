using System.Windows;
using System.Windows.Forms;
using JetBrains.Annotations;
using MarukoLib.Interop;
using MarukoLib.Lang.Concurrent;

namespace MarukoLib.UI
{

    public static class CursorUtils
    {

        private sealed class CursorVisibility
        {

            private readonly bool _initial;

            private readonly IAtomic<bool> _visible;

            public CursorVisibility(bool visible = true) => _visible = Atomics.Bool(_initial = visible);

            ~CursorVisibility() => AsVisible(_initial);

            // ReSharper disable once UnusedMethodReturnValue.Local
            public bool AsVisible(bool visible)
            {
                if (_visible.Set(visible) == visible) return false;
                SetCursorVisibility(visible);
                return true;
            }

        }

        public static void SetCursorVisibility(bool visible) => User32.ShowCursor(visible ? 1 : 0);

        public static void HideCursorOnFocus([NotNull] this UIElement element)
        {
            var visibility = new CursorVisibility();
            element.GotFocus += (sender, args) => visibility.AsVisible(false);
            element.LostFocus += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorOnFocus([NotNull] this Control control)
        {
            var visibility = new CursorVisibility();
            control.GotFocus += (sender, args) => visibility.AsVisible(false);
            control.LostFocus += (sender, args) => visibility.AsVisible(true);
            control.Disposed += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorOnActivated([NotNull] this Window window)
        {
            var visibility = new CursorVisibility();
            window.Activated += (sender, args) => visibility.AsVisible(false);
            window.Deactivated += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorOnActivated([NotNull] this Form form)
        {
            var visibility = new CursorVisibility();
            form.Activated += (sender, args) => visibility.AsVisible(false);
            form.Deactivate += (sender, args) => visibility.AsVisible(true);
            form.Disposed += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorInside([NotNull] this UIElement element)
        {
            var visibility = new CursorVisibility();
            element.MouseEnter += (sender, args) => visibility.AsVisible(false);
            element.MouseLeave += (sender, args) => visibility.AsVisible(true);
        }

        public static void HideCursorInside([NotNull] this Control control)
        {
            var visibility = new CursorVisibility();
            control.MouseEnter += (sender, args) => visibility.AsVisible(false);
            control.MouseLeave += (sender, args) => visibility.AsVisible(true);
            control.Disposed += (sender, args) => visibility.AsVisible(true);
        }

    }

}
