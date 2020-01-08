using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MarukoLib.Lang.Sequence
{

    public interface ISequence<out T>
    {

        T Next();

        void Reset();

    }

    public class DelegatedSequence<T> : ISequence<T>
    {

        [NotNull] private readonly Func<T> _nextFunc;

        [CanBeNull] private readonly Action _resetFunc;

        public DelegatedSequence([NotNull] Func<T> nextFunc, [CanBeNull] Action resetAction)
        {
            _nextFunc = nextFunc ?? throw new ArgumentNullException(nameof(nextFunc));
            _resetFunc = resetAction;
        }

        public T Next() => _nextFunc();

        public void Reset() => _resetFunc?.Invoke();

    }

    public sealed class EnumerableSequence<T> : ISequence<T>, IEnumerable<T>, IEnumerator<T>
    {

        private T _value;

        public EnumerableSequence([NotNull] ISequence<T> source) => Source = source ?? throw new ArgumentNullException(nameof(source));

        [NotNull] public ISequence<T> Source { get; }

        public T Next()
        {
            ((IEnumerator) this).MoveNext();
            return _value;
        }

        public void Reset() => Source.Reset();

        T IEnumerator<T>.Current => _value;

        object IEnumerator.Current => _value;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;

        bool IEnumerator.MoveNext()
        {
            _value = Source.Next();
            return true;
        }

        void IDisposable.Dispose() { }

    }

    public sealed class SyncedSequence<T> : ISequence<T>
    {

        private readonly object _lock = new object();

        public SyncedSequence([NotNull] ISequence<T> source) => Source = source ?? throw new ArgumentNullException(nameof(source));

        [NotNull] public ISequence<T> Source { get; }

        public T Next()
        {
            lock (_lock) return Source.Next();
        }

        public void Reset()
        {
            lock (_lock) Source.Reset();
        }

    }

    public static class SequenceExt
    {

        public static EnumerableSequence<T> AsEnumerable<T>(this ISequence<T> sequence) => new EnumerableSequence<T>(sequence);

        public static SyncedSequence<T> AsSynced<T>(this ISequence<T> sequence) => new SyncedSequence<T>(sequence);

    }
    
}
