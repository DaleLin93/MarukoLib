using Dragonfly;
using MarukoLib;
using System;
using System.Collections.Generic;
using System.Windows;
using MarukoLib.Containers;
using ToastNotifications.Messages;

namespace WheelchairGazeController.Interactors
{

    public class BrainInteractor : VirtualCursorInteractor, DragonflyAgent.IMessageHandler
    {

        private readonly object _lock = new object();

        private readonly DragonflyAgent _dragonfly;

        private readonly double _velocityScalar;

        private TimeStamped<Vector>? _lastVelocity = null;

        private Point _centeredPosition = new Point();

        public BrainInteractor(Window window, DragonflyAgent dragonfly, double velocityScalar = 1.0) : base(window, TimeSpan.FromMilliseconds(50))
        {
            _dragonfly = dragonfly;
            _dragonfly.Register(this);
            _velocityScalar = velocityScalar;
        }

        public override Point? CurrentPosition 
        {
            get
            {
                return Application.Current.DispatcherInvoke(() =>
                {
                    var now = (ulong)DateTimeUtils.CurrentTimeMillis;
                    var window = (Window)Owner;
                    if (!window.IsActive)
                        return null;

                    var centeredBounds = GetCenteredBoundsInPixel(window);
                    var centeredPosition = GetCenteredPosition(centeredBounds, now);

                    var position = new Point(centeredPosition.X - centeredBounds.X, centeredPosition.Y - centeredBounds.Y);

                    return new Point?(window.PointFromScreen(position));
                });
            }
        }

        public IEnumerable<int> SupportedMessageTypes => new int[] { MT.VELOCITY_DATA };

        public void Handle(Message message)
        {
            var now = (ulong)DateTimeUtils.CurrentTimeMillis;
            var velocity = message.GetData<typedefs.MDF_VELOCITY_DATA>().Vector;
            var length = velocity.Length;
            velocity.X /= length;
            velocity.Y /= length;
            Application.Current.DispatcherInvoke(() =>
            {
                var window = (Window)Owner;
                if (!window.IsActive)
                    return;

                var centeredBounds = GetCenteredBoundsInPixel(window);
                var centeredPosition = GetCenteredPosition(centeredBounds, now);

                /* Set to variables */
                lock (_lock)
                {
                    _lastVelocity = new TimeStamped<Vector>(now, velocity);
                    _centeredPosition = centeredPosition;
                }
            });
        }

        public override void Dispose()
        {
            _dragonfly.Unregister(this);
            base.Dispose();
        }

        private Rect GetCenteredBoundsInPixel(Window window)
        {
            var scale = GraphicsUtils.ScaleFactor;
            var windowRect = new Rect(new Point(window.Left, window.Top),
                new Size(window.Width * scale, window.ActualHeight * scale)); // to pixels
            var centeredX = (windowRect.Left + windowRect.Right) / 2;
            var centeredY = (windowRect.Top + windowRect.Bottom) / 2;
            return new Rect(windowRect.X - centeredX, windowRect.Y - centeredY, windowRect.Width, windowRect.Height);
        }

        private Point GetCenteredPosition(Rect centeredBounds, ulong now)
        {
            TimeStamped<Vector>? lastVelocity;
            Point centeredPos;
            lock (_lock)
            {
                lastVelocity = _lastVelocity;
                centeredPos = _centeredPosition;
            }
            if (lastVelocity.HasValue)
            {
                var timestampedVelocity = lastVelocity.Value;
                var dt = (now - timestampedVelocity.TimeStamp) / 1000.0;
                centeredPos.X += timestampedVelocity.Value.X * _velocityScalar * dt;
                centeredPos.Y += timestampedVelocity.Value.X * _velocityScalar * dt;
            }

            /* Clamp position */
            centeredPos.X = Math.Max(centeredBounds.Left, Math.Min(centeredPos.X, centeredBounds.Right));
            centeredPos.Y = Math.Max(centeredBounds.Top, Math.Min(centeredPos.Y, centeredBounds.Bottom));

            return centeredPos;
        }

    }

}
