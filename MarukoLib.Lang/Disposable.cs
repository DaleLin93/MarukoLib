using System;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public abstract class Disposable<T> : IDisposable
    {

        public sealed class Delegated : Disposable<T>
        {

            public Delegated(T value, [NotNull] Action<T> disposalAction) : base(value) => DisposalAction = disposalAction ?? throw new ArgumentNullException(nameof(disposalAction));

            [NotNull] public Action<T> DisposalAction { get; }

            protected override void DoDisposition() => DisposalAction(Value);

        }

        public sealed class NoAction : Disposable<T>
        {

            public NoAction(T value) : base(value) { }

            protected override void DoDisposition() { }

        }

        private bool _disposedValue = false;

        protected Disposable(T value) => Value = value;

        ~Disposable() => Dispose();

        [NotNull] public T Value { get; }

        public void Dispose()
        {
            lock(this)
                if (!_disposedValue)
                {
                    DoDisposition();
                    _disposedValue = true;
                }
        }

        protected abstract void DoDisposition();

    }

}
