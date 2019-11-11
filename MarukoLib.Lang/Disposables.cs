using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarukoLib.Lang.Concurrent;
using MarukoLib.Lang.Exceptions;

namespace MarukoLib.Lang
{

    public sealed class DisposablePool : IDisposable
    {

        private readonly AtomicBool _disposed = new AtomicBool(false);

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

    public class DelegatedDisposable : IDisposable
    {

        [NotNull] private readonly Action _delegate;

        private readonly bool _autoDisposable;

        private readonly AtomicBool _disposed = new AtomicBool(false);

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

}
