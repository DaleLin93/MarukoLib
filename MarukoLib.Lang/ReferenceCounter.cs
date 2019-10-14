using System;
using System.Threading;
using JetBrains.Annotations;
using MarukoLib.Lang.Concurrent;

namespace MarukoLib.Lang
{

    public sealed class ReferenceCounter
    {

        public sealed class Reference : IDisposable
        {

            private const int InitialState = 0, GrabbedState = 1, ReleasedState = 2;

            private readonly AtomicInt _state = new AtomicInt(InitialState);

            private readonly Action _grabAction, _releaseAction;

            internal Reference([NotNull] Action grabAction, [NotNull] Action releaseAction)
            {
                _grabAction = grabAction;
                _releaseAction = releaseAction;
            }

            ~Reference() => Release(true);

            public bool Grab()
            {
                if (!_state.CompareAndSet(InitialState, GrabbedState)) return false;
                _grabAction();
                return true;
            }

            public bool Release(bool forceRelease = false)
            {
                if (forceRelease ? _state.Set(ReleasedState) != GrabbedState : !_state.CompareAndSet(GrabbedState, ReleasedState)) return false;
                _releaseAction();
                return true;
            }

            void IDisposable.Dispose() => Release();

        }

        private long _ref;

        public long Count => Interlocked.CompareExchange(ref _ref, 0, 0);

        [Obsolete]
        public bool HasRef => IsReferred;

        public bool IsReferred => Count > 0;

        public Reference Ref(bool grab = true)
        {
            var @ref = new Reference(RefIncrement, RefDecrement);
            if (grab) @ref.Grab();
            return @ref;
        }

        public void Do(Action action)
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (Ref()) action();
        }

        private void RefIncrement() => Interlocked.Increment(ref _ref);

        private void RefDecrement() => Interlocked.Decrement(ref _ref);

    }

}
