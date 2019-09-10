using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using MarukoLib;

namespace WheelchairGazeController.Interactors
{

    public class KeyboardInteractor : VirtualCursorInteractor
    {

        private static readonly Key[] CheckingKeys = { Key.Up, Key.Down, Key.Left, Key.Right };

        private readonly Timer _timer;

        private readonly bool[] _keyPressedStates = new bool[CheckingKeys.Length];

        private readonly int[] _continuousDectectionCounts = new int[CheckingKeys.Length];

        private Point? _currentPositionInPixels;

        private Point? _relativePosition;

        public KeyboardInteractor(Window window, TimeSpan detectionPeriod) : base(window)
        {
            _timer = new Timer(DoTick, null, detectionPeriod, detectionPeriod);
        }

        public override Point? CurrentPosition
        {
            get
            {
                return _relativePosition; 
            }
        }

        public static Key? GetPressedKey()
        {
            return null;
        }

        public override void Dispose()
        {
            _timer.Dispose();
            base.Dispose();
        }

        private new void DoTick(object state)
        {
            /* STA for accessing UI Elements */
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = (Window)Owner;
                if (!window.IsActive) // Window may disposing
                    return;

                bool hasPressedKey = false;
                for (var i = 0; i < CheckingKeys.Length; i++)
                    if (_keyPressedStates[i] = Keyboard.IsKeyDown(CheckingKeys[i]))
                    {
                        hasPressedKey = true;
                        _continuousDectectionCounts[i]++;
                    }
                    else
                        _continuousDectectionCounts[i] = 0;

                if (!hasPressedKey)
                    return;

                var scale = GraphicsUtils.ScaleFactor; 
                var bounds = new Rect(new Point(window.Left, window.Top), new Size(window.Width * scale, window.ActualHeight * scale)); // to pixels
                var currentPointInPixels = _currentPositionInPixels ?? new Point((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);

                /* Handle keys */
                for (int i = 0; i < CheckingKeys.Length; i++)
                {
                    if (!_keyPressedStates[i])
                        continue;
                    var offset = Math.Min(_continuousDectectionCounts[i], 20);
                    switch (CheckingKeys[i])
                    {
                        case Key.Left:
                            currentPointInPixels.X -= offset;
                            break;
                        case Key.Up:
                            currentPointInPixels.Y -= offset;
                            break;
                        case Key.Right:
                            currentPointInPixels.X += offset;
                            break;
                        case Key.Down:
                            currentPointInPixels.Y += offset;
                            break;
                    }
                }

                /* Clamp position */
                currentPointInPixels.X = Math.Max(bounds.Left, Math.Min(currentPointInPixels.X, bounds.Right));
                currentPointInPixels.Y = Math.Max(bounds.Top, Math.Min(currentPointInPixels.Y, bounds.Bottom));

                /* Set to variables */
                _currentPositionInPixels = currentPointInPixels;
                _relativePosition = window.PointFromScreen(currentPointInPixels);

                /* Must call NotifyPositionChanged while position is self-managed */
                NotifyPositionChanged();
            });
        }

    }

}
