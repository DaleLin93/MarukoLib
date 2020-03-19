using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MarukoLib.Interop.Native;

namespace MarukoLib.Interop
{

    public class WindowBlur
    {

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(WindowBlur), new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty WindowBlurProperty = DependencyProperty.RegisterAttached(
            "WindowBlur", typeof(WindowBlur), typeof(WindowBlur), new PropertyMetadata(null, OnWindowBlurChanged));

        private Window _window;

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static bool GetIsEnabled(DependencyObject element) => (bool) element.GetValue(IsEnabledProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                if (true.Equals(e.OldValue))
                {
                    GetWindowBlur(window)?.Detach();
                    window.ClearValue(WindowBlurProperty);
                }
                if (true.Equals(e.NewValue))
                {
                    var blur = new WindowBlur();
                    blur.Attach(window);
                    window.SetValue(WindowBlurProperty, blur);
                }
            }
        }

        public static void SetWindowBlur(DependencyObject element, WindowBlur value) => element.SetValue(WindowBlurProperty, value);

        public static WindowBlur GetWindowBlur(DependencyObject element) => (WindowBlur) element.GetValue(WindowBlurProperty);

        private static void OnWindowBlurChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                (e.OldValue as WindowBlur)?.Detach();
                (e.NewValue as WindowBlur)?.Attach(window);
            }
        }

        private void Attach(Window window)
        {
            _window = window;
            var source = (HwndSource) PresentationSource.FromVisual(window);
            if (source == null)
            {
                window.SourceInitialized += OnSourceInitialized;
            }
            else
            {
                AttachCore();
            }
        }

        private void Detach()
        {
            try
            {
                DetachCore();
            }
            finally
            {
                _window = null;
            }
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            ((Window) sender).SourceInitialized -= OnSourceInitialized;
            AttachCore();
        }

        private void AttachCore() => EnableBlur(_window);

        private void DetachCore() => _window.SourceInitialized += OnSourceInitialized;

        private static void EnableBlur(Window window)
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };
            User32.SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

    }

    namespace Native
    {

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        public enum AccentState
        {
            ACCENT_DISABLED,
            ACCENT_ENABLE_GRADIENT,
            ACCENT_ENABLE_TRANSPARENTGRADIENT,
            ACCENT_ENABLE_BLURBEHIND,
            ACCENT_INVALID_STATE
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum WindowCompositionAttribute
        {
            // 省略其他未使用的字段
            WCA_ACCENT_POLICY = 19
            // 省略其他未使用的字段
        }
    }
}
