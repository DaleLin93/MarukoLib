using System;
using System.Windows;
using System.Windows.Threading;

namespace WheelchairGazeController.Interactors
{
    public class NoopInteractor : AbstractInteractor
    {

        public NoopInteractor(FrameworkElement owner) : base(owner, TimeSpan.FromDays(1)) { }

        public override Point? CurrentPosition => null;

        protected override void PostCreateObj(InteractableObject obj) { }

        protected override void PreDestroyObj(InteractableObject obj) { }

    }

}
