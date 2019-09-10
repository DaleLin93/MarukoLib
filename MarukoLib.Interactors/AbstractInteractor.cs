using MarukoLib;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WheelchairGazeController.Interactors
{

    public abstract class AbstractInteractor : IInteractor
    {

        private static readonly TimeSpan DefaultTickingInterval = TimeSpan.FromMilliseconds(100);

        private readonly object _registrationLock = new object();

        private readonly IDictionary<FrameworkElement, InteractableObject> _interactableObjects = new Dictionary<FrameworkElement, InteractableObject>();

        private readonly DispatcherObject _dispatcher;

        /// <summary>
        /// Intrinsic timer to update states and trigger actions
        /// </summary>
        private readonly Timer _timer;

        private long _activedAtTicks = 0;

        private bool _triggered = false;

        protected AbstractInteractor(FrameworkElement owner, TimeSpan? tickingPeriod = null) : this(owner, owner, tickingPeriod) { }

        protected AbstractInteractor(FrameworkElement owner, DispatcherObject dispatcher, TimeSpan? tickingPeriod = null)
        {
            Owner = owner;
            _dispatcher = dispatcher;
            var actualTickingPeriod = tickingPeriod ?? DefaultTickingInterval;
            _timer = new Timer(new TimerCallback(DoTick), null, actualTickingPeriod, actualTickingPeriod);
        }

        public FrameworkElement Owner { get; }

        public abstract Point? CurrentPosition { get; }

        public bool HasActived => ActivedObject != null;

        public InteractableObject ActivedObject { get; private set; } = null;

        public InteractableObject FindRegisteredElement(FrameworkElement element)
        {
            if (element == null)
                throw new NullReferenceException("element cannot be null.");
            lock (_registrationLock)
                return _interactableObjects.ContainsKey(element) ? _interactableObjects[element] : null;
        }

        public InteractableObject RegisterElement(FrameworkElement element, TimeSpan? triggerDelay = null)
        {
            if (element == null)
                throw new NullReferenceException("element cannot be null.");
            lock (_registrationLock) /* Lock to avoid multiple registration */
            {
                /* Existed */
                if (_interactableObjects.ContainsKey(element))
                    return _interactableObjects[element];
                /* Register */
                var result = new InteractableObject(element, triggerDelay);
                PostCreateObj(_interactableObjects[element] = result);
                return result;
            }
        }

        public bool UnRegisterElement(FrameworkElement element)
        {
            if (element == null)
                throw new NullReferenceException("element cannot be null.");
            lock (_registrationLock)
            {
                if (!_interactableObjects.ContainsKey(element))
                    return false;
                var obj = _interactableObjects[element];
                PreDestroyObj(obj);
                obj.metadata.Clear();
                _interactableObjects.Remove(element);
                return true;
            }
        }

        public void UnRegisterAll()
        {
            lock (_registrationLock)
            {
                foreach (var entry in _interactableObjects)
                {
                    PreDestroyObj(entry.Value);
                    entry.Value.metadata.Clear();
                }
                _interactableObjects.Clear();
            }
        }

        public virtual void Dispose()
        {
            _timer.Dispose();
        }

        protected abstract void PostCreateObj(InteractableObject obj);

        protected abstract void PreDestroyObj(InteractableObject obj);

        protected virtual void SetActive(InteractableObject obj)
        {
            var actived = ActivedObject;
            if (actived != null && actived != obj)
                SetInactive(actived);
            if (obj == null)
                return;
            ActivedObject = obj;
            _activedAtTicks = DateTime.Now.Ticks;
            _triggered = false;
            DoAction(obj.RaiseActivedEvent);
        }

        protected virtual bool SetInactive(InteractableObject obj)
        {
            var actived = ActivedObject;
            if (obj == actived)
            {
                DoAction(actived.RaiseDeactivedEvent);
                ActivedObject = null;
                _activedAtTicks = -1;
                _triggered = true;
                return true;
            }
            return false;
        }

        protected virtual bool TriggerAction(InteractableObject obj)
        {
            var actived = ActivedObject;
            if (obj == actived && !_triggered)
            {
                _triggered = true;
                DoAction(actived.RaiseTriggeredEvent);
                return true;
            }
            return false;
        }

        protected virtual void DoTick(object state)
        {
            var actived = ActivedObject;
            if (actived != null && !_triggered)
            {
                var percentage = (DateTime.Now.Ticks - _activedAtTicks) / (double)(actived.triggerDelay?.Ticks ?? 0);
                if (percentage >= 1)
                    TriggerAction(actived);
                else
                    DoAction(() => actived.RaiseTickEvent(percentage));
            }
        }

        protected virtual void DoAction(Action action)
        {
            if (action != null)
                if (_dispatcher == null)
                    action();
                else
                    ThreadUtils.DispatchBy(action, _dispatcher);
        }

    }

}
