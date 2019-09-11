using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public sealed class DisposablePool : IDisposable
    {

        private readonly LinkedList<IDisposable> _disposables = new LinkedList<IDisposable>();

        ~DisposablePool() => DisposeAll();

        public T Add<T>(T disposable) where T : IDisposable
        {
            lock (_disposables)
                _disposables.AddFirst(disposable);
            return disposable;
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

        void IDisposable.Dispose() => DisposeAll();

    }

    public class DelegatedDisposable : IDisposable
    {

        [NotNull] private readonly Action _delegate;

        private readonly bool _autoDisposable;

        private int _disposed = 0;

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
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
                _delegate();
        }
    }

}
