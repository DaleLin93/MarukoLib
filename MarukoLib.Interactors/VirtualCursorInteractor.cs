using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarukoLib;

namespace WheelchairGazeController.Interactors
{
    public abstract class VirtualCursorInteractor : AbstractInteractor
    {

        private class HitTest
        {

            private readonly Point _point;

            public HitTest(Point point)
            {
                _point = point;
            }

            private static HitTestFilterBehavior HitTestFilter(DependencyObject obj)
            {
                if (obj is FrameworkElement)
                {
                    var element = (FrameworkElement)obj;
                    if (!element.IsVisible)
                        return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                    else if (!element.IsHitTestVisible)
                        return HitTestFilterBehavior.ContinueSkipSelf;
                    return HitTestFilterBehavior.Continue;
                }
                else
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }

            public FrameworkElement Execute(FrameworkElement element)
            {
                FrameworkElement hit = null;
                HitTestResultCallback resultCallback = result =>
                {
                    hit = (FrameworkElement)result?.VisualHit;
                    return HitTestResultBehavior.Stop;
                };
                VisualTreeHelper.HitTest(element, HitTestFilter, resultCallback, new PointHitTestParameters(_point));
                return hit;
            }
        }

        private readonly Timer _timer;

        // Temporary variables

        private Point? _lastPosition;

        private bool _dirty = true;

        protected VirtualCursorInteractor(FrameworkElement owner, TimeSpan? refreshPeriod = null, TimeSpan? interactorTickingPeriod = null) : base(owner, interactorTickingPeriod)
        {
            _timer = refreshPeriod == null ? null : new Timer(state => Update(), null, TimeSpan.Zero, refreshPeriod.Value);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }

        protected void NotifyPositionChanged()
        {
            _dirty = true;
        }

        protected override void PostCreateObj(InteractableObject obj) { }

        protected override void PreDestroyObj(InteractableObject obj) { }

        protected override void DoTick(object state)
        {
            if (_timer == null && _dirty && Update()) _dirty = false;
            base.DoTick(state);
        }

        protected bool Update()
        {
            if (!Owner.IsVisible)
                return false;
            var position = CurrentPosition;
            if (Equals(_lastPosition, position))
                return true;
            InteractableObject interactableObject = null;
            if (position.HasValue)
            {
                var hitTest = new HitTest(position.Value);
                var hitElement = Owner.DispatcherInvoke(owner => hitTest.Execute(owner));
                while (hitElement != null && (interactableObject = hitElement == null ? null : FindRegisteredElement(hitElement)) == null)
                {
                    var parent = hitElement.Parent;
                    hitElement = parent is FrameworkElement ? (FrameworkElement) parent : null;
                }
            }
            if (interactableObject != ActivedObject)
                SetActive(interactableObject);
            _lastPosition = position;
            return true;
        }

    }

}
