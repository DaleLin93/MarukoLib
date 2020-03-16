using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarukoLib.Lang.Concurrent;
using MarukoLib.Lang.Exceptions;

namespace MarukoLib.Lang
{

    public sealed class DisposablePool : IDisposable
    {

        private readonly AtomicBool _disposed = Atomics.Bool(false);

        private readonly LinkedList<IDisposable> _disposables = new LinkedList<IDisposable>();

        ~DisposablePool() => DisposeAll();

        public bool AddIfDisposable(object obj)
        {
            if (!(obj is IDisposable disposable)) return false;
            Add(disposable);
            return true;
        }

        public void Add(IDisposable disposable)
        {
            if (_disposed.Value) throw new StateException("DisposablePool is already disposed.");
            lock (_disposables) _disposables.AddFirst(disposable);
        }

        public void DisposeAll()
        {
            lock (_disposables)
            {
                foreach (var disposable in _disposables)
                    disposable.Dispose();
                _disposables.Clear();
            }
        }

        public void Dispose()
        {
            if (_disposed.CompareAndSet(false, true))
                DisposeAll();
        }
    }

    public sealed class NoOpDisposable : IDisposable
    {

        public static readonly NoOpDisposable Instance = new NoOpDisposable();

        private NoOpDisposable() { }

        public void Dispose() { }

    }

    public class DelegatedDisposable : IDisposable
    {

        [NotNull] private readonly Action _delegate;

        private readonly bool _autoDisposable;

        private readonly AtomicBool _disposed = Atomics.Bool(false);

        public DelegatedDisposable([NotNull] Action @delegate, bool autoDisposable = true)
        {
            _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
            _autoDisposable = autoDisposable;
        }

        ~DelegatedDisposable()
        {
            if (_autoDisposable) Dispose();
        }

        public void Dispose()
        {
            if (_disposed.CompareAndSet(false, true)) _delegate();
        }

    }

    public abstract class Disposable<T> : IDisposable
    {

        public sealed class Delegated : Disposable<T>
        {

            public Delegated(T value, [NotNull] Action<T> disposalAction) : base(value) => DisposalAction = disposalAction ?? throw new ArgumentNullException(nameof(disposalAction));

            [NotNull] public Action<T> DisposalAction { get; }

            protected override void Dispose(bool deconstruct) => DisposalAction(Value);

        }

        public sealed class NoOp : Disposable<T>
        {

            public NoOp(T value) : base(value) { }

            protected override void Dispose(bool deconstruct) { }

        }

        private bool _disposed;

        protected Disposable(T value) => Value = value;

        ~Disposable() => Dispose0(true);

        public static Disposable<T> Of([CanBeNull] T value, [CanBeNull] Action<T> disposalAction) 
            => disposalAction == null ? (Disposable<T>) new NoOp(value) : new Delegated(value, disposalAction);

        [NotNull] public T Value { get; }

        public void Dispose() => Dispose0(false);

        protected abstract void Dispose(bool deconstruct);

        private void Dispose0(bool deconstruct)
        {
            lock (this)
                if (!_disposed)
                {
                    Dispose(deconstruct);
                    _disposed = true;
                }
        }

    }

    public static class Disposables
    {

        public static IDisposable For(Action action) => action == null ? (IDisposable) NoOpDisposable.Instance : new DelegatedDisposable(action, true);

    }

}
