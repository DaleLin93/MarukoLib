using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MarukoLib.UI
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class UIExtensions
    {

        public static double GetVisualScaling(this Visual visual)
        {
            var transformMatrix = PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice;
            return transformMatrix?.M11 ?? 1;
        }

        public static bool FindAndSelectFirst(this Selector selector, Predicate<object> predicate, int? defaultIndex = null)
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

        public static bool FindAndSelectFirstByString(this Selector selector, string itemStr, 
            int? defaultIndex = null, StringComparison comparison = StringComparison.InvariantCulture)
        {
            var predicate = itemStr == null ? (Predicate<object>) null : item => itemStr.Equals(item.ToString(), comparison);
            return FindAndSelectFirst(selector, predicate, defaultIndex);
        }

        public static bool FindAndSelectFirstByTag(this Selector selector, object tagObj,
            int? defaultIndex = null, bool compareRef = true)
        {
            Predicate<object> predicate;
            if (compareRef && (tagObj == null || !tagObj.GetType().IsValueType))
                predicate = item => item is FrameworkElement control && ReferenceEquals(control.Tag, tagObj);
            else
                predicate = item => item is FrameworkElement control && Equals(control.Tag, tagObj);
            return FindAndSelectFirst(selector, predicate, defaultIndex);
        }

        public static RenderTargetBitmap RenderImage(this FrameworkElement ui, int width, int height, double dpi = 96d)
        {
            var bmp = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            bmp.Render(ui);
            return bmp;
        }

        public static bool TryGetScreen(this Window window, out Screen screenInside)
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

        public static void MoveToScreen(this Window window, Func<Point, Point, bool> filterFunc)
        {
            var centerPoint = window.PointToScreen(new Point(window.ActualWidth / 2, window.ActualHeight / 2));
            var targetScreen = Screen.AllScreens
                .Where(screen => !screen.Bounds.Contains(centerPoint.RoundToSdPoint()))
                .Select(screen => new
                {
                    Screen = screen, 
                    Center = new Point(screen.Bounds.X + screen.Bounds.Width / 2, screen.Bounds.Y + screen.Bounds.Height / 2)
                })
                .Where(obj => filterFunc(centerPoint, obj.Center))
                .OrderBy(obj => obj.Center.ManhattanDistance(centerPoint))
                .FirstOrDefault();
            if (targetScreen == null) return;
            var screenCenterPoint = targetScreen.Center.Divide(GraphicsUtils.Scale);
            window.WindowState = WindowState.Normal;
            window.Left = screenCenterPoint.X - 5;
            window.Top = screenCenterPoint.Y - 5;
            window.Width = 10;
            window.Height = 10;
            window.WindowState = WindowState.Maximized;
        }

    }
}
