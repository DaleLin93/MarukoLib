using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace WheelchairGazeController.Interactors
{

    public interface IInteractor : IDisposable
    {

        FrameworkElement Owner { get; }

        /// <summary>
        /// Relative position should be retruned.
        /// Value can be null if out of screen.
        /// </summary>
        Point? CurrentPosition { get; }

        bool HasActived { get; }

        InteractableObject ActivedObject { get; }

        InteractableObject RegisterElement(FrameworkElement el, TimeSpan? triggerDelay = null);

        bool UnRegisterElement(FrameworkElement el);

        void UnRegisterAll();

    }

    public class InteractableObject
    {

        public sealed class MetaKey<T>
        {

            public bool HasValue(InteractableObject obj)
            {
                return obj.metadata.Contains(this) && obj.metadata[this] is T;
            }

            public T Get(InteractableObject obj, T defaultValue = default(T))
            {
                if (obj.metadata.Contains(obj))
                {
                    object value = obj.metadata[this];
                    return value is T ? (T)value : defaultValue;
                }
                else
                    return defaultValue;
            }

            public void Set(InteractableObject obj, T value)
            {
                obj.metadata[this] = value;
            }

            public bool RemoveValue(InteractableObject obj)
            {
                if (HasValue(obj))
                {
                    obj.metadata.Remove(this);
                    return true;
                }
                return false;
            }

        }

        public class TickEventArgs
        {

            public TickEventArgs(double percentage)
            {
                Percentage = percentage;
            }

            public double Percentage { get; private set; }

        }

        public event EventHandler Actived;

        public event EventHandler Deactived;

        public event EventHandler Triggered;

        public event EventHandler<TickEventArgs> Tick;

        public readonly FrameworkElement element;

        public readonly TimeSpan? triggerDelay;

        public readonly IDictionary metadata = new Dictionary<object, object>();

        public InteractableObject(FrameworkElement element, TimeSpan? triggerDelay)
        {
            this.element = element;
            this.triggerDelay = triggerDelay;
        }

        public object this[object key]
        {
            get
            {
                if (!metadata.Contains(key))
                    return null;
                return metadata[key];
            }
            set
            {
                metadata[key] = value;
            }
        }

        public void HandleEvents(Action delayedAction = null, Action activeAction = null, Action deactiveAction = null)
        {
            if (activeAction != null)
                Actived += (sender, e) => activeAction();
            if (deactiveAction != null)
                Deactived += (sender, e) => deactiveAction();
            if (delayedAction != null)
                Triggered += (sender, e) => delayedAction();
        }

        public void RaiseActivedEvent()
        {
            Actived?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseDeactivedEvent()
        {
            Deactived?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseTriggeredEvent()
        {
            Triggered?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseTickEvent(double percentage)
        {
            Tick?.Invoke(this, new TickEventArgs(percentage));
        }

    }

}
