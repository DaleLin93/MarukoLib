using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarukoLib;

namespace WheelchairGazeController.Interactors
{

    public class VirtualMouseInteractor : VirtualCursorInteractor
    {

        private struct Point2I
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point2I lpPoint);

        public VirtualMouseInteractor(Window window, TimeSpan? refreshPeriod = null) : base(window, refreshPeriod) { }

        public override Point? CurrentPosition
        {
            get
            {
                return Owner.DispatcherInvoke(owner =>
                {
                    var window = (Window)Owner; // safe to do so
                    if (!window.IsActive)
                        return null;
                    var scale = GraphicsUtils.ScaleFactor; // handling gui scaling feature
                    var bounds = new Rect(new Point(window.Left, window.Top), new Size(window.Width * scale, window.ActualHeight * scale));
                    GetCursorPos(out Point2I cursor);
                    var point = new Point(cursor.X, cursor.Y);
                    if (!bounds.Contains(point))
                        return null;

                    // IMPORTANT: convert from absolute screen coordinate to element relative coordinate.
                    return (Point?)window.PointFromScreen(point);
                });
            }
        }

    }

}
