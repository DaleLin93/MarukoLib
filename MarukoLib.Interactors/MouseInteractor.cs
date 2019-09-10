using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WheelchairGazeController.Interactors
{
    public class MouseInteractor : AbstractInteractor
    {

        private static readonly InteractableObject.MetaKey<Delegate[]> EventHandlersKey =
            new InteractableObject.MetaKey<Delegate[]>();

        public MouseInteractor(FrameworkElement owner) : base(owner) { }

        public override Point? CurrentPosition { get { return Mouse.GetPosition(Owner); } }

        protected override void PostCreateObj(InteractableObject obj)
        {
            MouseEventHandler mouseEnterHandler = (sender, e) => SetActive(obj);
            MouseEventHandler mouseLeaveHandler = (sender, e) => SetInactive(obj);
            MouseButtonEventHandler mouseUpHandler = (sender, e) => TriggerAction(obj);
            EventHandlersKey.Set(obj, new Delegate[] { mouseEnterHandler, mouseLeaveHandler, mouseUpHandler });
            obj.element.MouseEnter += mouseEnterHandler;
            obj.element.MouseLeave += mouseLeaveHandler;
            obj.element.MouseUp += mouseUpHandler;
        }

        protected override void PreDestroyObj(InteractableObject obj)
        {
            var handlers = EventHandlersKey.Get(obj);
            obj.element.MouseEnter -= (MouseEventHandler) handlers[0];
            obj.element.MouseLeave -= (MouseEventHandler) handlers[1];
            obj.element.MouseUp -= (MouseButtonEventHandler) handlers[2];
        }

    }

}
