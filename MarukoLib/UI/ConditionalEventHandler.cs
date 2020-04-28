using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.UI
{

    public interface IConditionalEventHandler<in T> : IDisposable
    {

        bool IsBlocked { get; }

        IDisposable Block();

        bool Attach(T target);

        bool Detach(T element);

    }

    public abstract class ConditionalEventHandler<T> : IConditionalEventHandler<T>
    {

        protected readonly ReferenceCounter EventLock = new ReferenceCounter();

        protected readonly LinkedList<T> Targets = new LinkedList<T>();

        ~ConditionalEventHandler() => Dispose();

        public bool IsBlocked => EventLock.IsReferred;

        public IDisposable Block() => EventLock.Ref();

        public bool Attach(T target)
        {
            lock (Targets)
            {
                if (Targets.Contains(target)) return false;
                Targets.AddLast(target);
                PostAttach(target);
                return true;
            }
        }

        public bool Detach(T target)
        {
            lock (Targets)
            {
                if (!Targets.Remove(target)) return false;
                PostDetach(target);
                return true;
            }
        }

        protected abstract void PostAttach(T target);

        protected abstract void PostDetach(T target);

        public void Dispose()
        {
            lock (Targets)
            {
                foreach (var target in Targets)
                    PostDetach(target);
                Targets.Clear();
            }
        }

    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ConditionalUIEventHandler<T> : ConditionalEventHandler<T> where T : UIElement 
    {

        [NotNull] private readonly RoutedEvent _event;

        [NotNull] private readonly Delegate _eventHandler;

        [NotNull] private readonly RoutedEventHandler _routedEventHandler;

        public ConditionalUIEventHandler([NotNull] RoutedEvent @event, [NotNull] Delegate eventHandler)
        {
            if (@event.HandlerType != eventHandler.GetType())
                throw new ArgumentException($"Invalid event handler type. expected: '{@event.HandlerType}', actual: '{eventHandler.GetType()}'.");
            if (!@event.OwnerType.IsAssignableFrom(typeof(T)))
                throw new ArgumentException($"Invalid event owner type. expected: '{@event.OwnerType}', actual: '{typeof(T)}'.");
            _event = @event ?? throw new ArgumentNullException(nameof(@event));
            _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
            _routedEventHandler = EventHandler;
        }

        protected override void PostAttach(T target) => target.AddHandler(_event, _routedEventHandler);

        protected override void PostDetach(T target) => target.RemoveHandler(_event, _routedEventHandler);

        protected void EventHandler(object sender, RoutedEventArgs e)
        {
            if (IsBlocked) return;
            _eventHandler.DynamicInvoke(sender, e);
        }

    }

    public static class ConditionalEventHandlers
    {

        public static IConditionalEventHandler<Selector> SelectorOnSelectionChanged([NotNull] SelectionChangedEventHandler eventHandler)
            => new ConditionalUIEventHandler<Selector>(Selector.SelectionChangedEvent, eventHandler);

        public static IConditionalEventHandler<TextBoxBase> TextBoxOnTextChanged([NotNull] TextChangedEventHandler eventHandler)
            => new ConditionalUIEventHandler<TextBoxBase>(TextBoxBase.TextChangedEvent, eventHandler);

    }

}
