using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Tobii.Interaction;
using Tobii.Interaction.Wpf;
using WheelchairGazeController.Filters;

namespace WheelchairGazeController.Interactors
{
    public class HybridInteractor : AbstractInteractor
    {

        private readonly object _lock = new object();

        private readonly PointFilter _pointFilter;

        private readonly WpfInteractorAgent _agent;

        /* Temporary variables */

        private Point _lastGazePoint;

        private bool _gazeLost = true;

        public HybridInteractor(FrameworkElement owner, WpfInteractorAgent agent, PointFilter pointFilter = null) : base(owner)
        {
            _agent = agent;
            _pointFilter = pointFilter ?? IdentityPointFilter.INSTANCE;
        }

        public override Point? CurrentPosition { get { return _gazeLost ? null : (Point?)_lastGazePoint; } }

        protected override void PostCreateObj(InteractableObject obj)
        {
            /* Gaze part */
            _agent.AddInteractorFor(obj.element).WithGazeAware().HasGazeChanged += (sender, e) =>
            {
                if (e.HasGaze)
                    SetActive(obj);
                else
                    SetInactive(obj);
            };
            /* Mouse part */
            obj.element.MouseEnter += (sender, e) => SetActive(obj);
            obj.element.MouseLeave += (sender, e) => SetInactive(obj);
            obj.element.MouseUp += (sender, e) => TriggerAction(obj);
        }

        protected override void PreDestroyObj(InteractableObject obj) { }

        protected override void SetActive(InteractableObject obj)
        {
            lock (_lock)
            {
                base.SetActive(obj);
            }
        }

        protected override bool SetInactive(InteractableObject obj)
        {
            lock (_lock)
            {
                return base.SetInactive(obj);
            }
        }

        protected override bool TriggerAction(InteractableObject obj)
        {
            lock (_lock)
            {
                return base.TriggerAction(obj);
            }
        }

    }

}
