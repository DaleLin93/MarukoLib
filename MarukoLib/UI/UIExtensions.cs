using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using MarukoLib.Graphics;

namespace MarukoLib.UI
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class UIExtensions
    {

        public static void SetHidden([NotNull] this UIElement element, bool hidden)
            => element.Visibility = hidden ? Visibility.Hidden : Visibility.Visible;

        public static void SetCollapsed([NotNull] this UIElement element, bool collapsed)
            => element.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;

        public static T FindFirstVisualChild<T>([NotNull] this DependencyObject root) where T : DependencyObject 
            => FindFirstVisualChild(root, obj => obj is T, out var resultObj) ? (T)resultObj : null;

        public static bool FindFirstVisualChild<T>([NotNull] this DependencyObject root, [NotNull] Predicate<T> filter, out T result) where T : DependencyObject
        {
            if (FindFirstVisualChild(root, obj => obj is T t && filter(t), out var resultObj))
            {
                result = (T)resultObj;
                return true;
            }
            result = default;
            return false;
        }

        public static bool FindFirstVisualChild([NotNull] this DependencyObject root, [NotNull] Predicate<DependencyObject> filter, out DependencyObject result)
        {
            var queue = new Queue<DependencyObject>(new[] { root });
            do
            {
                var item = queue.Dequeue();
                if (filter(item))
                {
                    result = item;
                    return true;
                }
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
                    queue.Enqueue(VisualTreeHelper.GetChild(item, i));
            } while (queue.Count > 0);
            result = default;
            return false;
        }

        public static bool MoveFocus([NotNull] this UIElement focusedElement, FocusNavigationDirection direction) => focusedElement.MoveFocus(new TraversalRequest(direction));

        public static double GetVisualScaling([NotNull] this Visual visual) => PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice.M11 ?? 1;

        public static bool IsChecked([CanBeNull] this ToggleButton toggleButton, bool defaultVal = false) => toggleButton?.IsChecked ?? defaultVal;

        public static bool FindAndSelectFirst<T>([NotNull] this Selector selector, Func<T, object> extractFunc, object targetValue, int? defaultIndex = null)
            => FindAndSelectFirst(selector, item => item is T t && Equals(extractFunc(t), targetValue), defaultIndex);

        public static bool FindAndSelectFirst([NotNull] this Selector selector, Predicate<object> predicate, int? defaultIndex = null)
        {
            if (predicate != null)
            {
                var i = 0;
                foreach (var item in selector.Items)
                {
                    if (predicate(item))
                    {
                        selector.SelectedIndex = i;
                        return true;
                    }
                    i++;
                }
            }
            if (defaultIndex != null)
                selector.SelectedIndex = defaultIndex.Value;
            return false;
        }

        public static bool FindAndSelectFirstByString([NotNull] this Selector selector, string str, 
            int? defaultIndex = null, StringComparison comp = StringComparison.InvariantCulture) 
            => FindAndSelectFirst(selector, str == null ? (Predicate<object>)null : item => str.Equals(item.ToString(), comp), defaultIndex);

        public static bool FindAndSelectFirstByTag([NotNull] this Selector selector, [CanBeNull] Predicate<object> tagPredicate, int? defaultIndex = null)
        {
            Predicate<object> predicate;
            if (tagPredicate == null)
                predicate = null;
            else
                predicate = item => item is FrameworkElement control && tagPredicate(control.Tag);
            return FindAndSelectFirst(selector, predicate, defaultIndex);
        }

        public static bool FindAndSelectFirstByTag([NotNull] this Selector selector, object tagObj,
            int? defaultIndex = null, bool compareRef = true)
        {
            Predicate<object> predicate;
            if (compareRef && (tagObj == null || !tagObj.GetType().IsValueType))
                predicate = item => item is FrameworkElement control && ReferenceEquals(control.Tag, tagObj);
            else
                predicate = item => item is FrameworkElement control && Equals(control.Tag, tagObj);
            return FindAndSelectFirst(selector, predicate, defaultIndex);
        }

        [NotNull]
        public static RenderTargetBitmap RenderImage([NotNull] this Visual visual, int width, int height, double dpi = 96d)
        {
            var bmp = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            bmp.Render(visual);
            return bmp;
        }

        public static bool TryGetScreen([NotNull] this Window window, out Screen screenInside)
        {
            var point = new Point(window.Left, window.Top);
            foreach (var screen in Screen.AllScreens)
                if (screen.WorkingArea.Contains((int)point.X, (int)point.Y))
                {
                    screenInside = screen;
                    return true;
                }
            screenInside = null;
            return false;
        }

        public static void MoveToScreen([NotNull] this Window window, [NotNull] Func<Point, Point, bool> filterFunc)
        {
            var centerPoint = window.PointToScreen(new Point(window.ActualWidth / 2, window.ActualHeight / 2));
            var targetScreen = (from screen in Screen.AllScreens
                where !screen.Bounds.Contains(centerPoint.RoundToSdPoint())
                let center = new Point(screen.Bounds.X + screen.Bounds.Width / 2, screen.Bounds.Y + screen.Bounds.Height / 2)
                where filterFunc(centerPoint, center)
                orderby center.ManhattanDistance(centerPoint)
                select new
                {
                    Screen = screen,
                    Center = center
                }).FirstOrDefault();
            if (targetScreen == null) return;
            var screenCenterPoint = targetScreen.Center.Div(DpiUtils.Scale);
            window.WindowState = WindowState.Normal;
            window.Left = screenCenterPoint.X - 5;
            window.Top = screenCenterPoint.Y - 5;
            window.Width = 10;
            window.Height = 10;
            window.WindowState = WindowState.Maximized;
        }

    }

}
