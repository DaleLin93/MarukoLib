using System;
using System.Threading;
using JetBrains.Annotations;
using MarukoLib.Lang.Sequence;

namespace MarukoLib.Lang.Concurrent
{

    public interface IFiringControl
    {

        bool Check();

        void Reset();

    }

    public class DelayedFiringControl : IFiringControl
    {

        private readonly object _lock = new object();

        private long? _timestamp;

        public DelayedFiringControl(long delayMillis, bool delayFirst = true)
        {
            DelayMillis = delayMillis;
            DelayFirst = delayFirst;
        }

        public long DelayMillis { get; }

        public bool DelayFirst { get; }

        public bool Check()
        {
            lock (_lock)
            {
                var now = DateTimeUtils.CurrentTimeMillis;
                if (_timestamp == null || _timestamp.Value + DelayMillis < now)
                {
                    _timestamp = now;
                    return true;
                }
                return false;
            }
        }

        public void Reset()
        {
            lock (_lock)
                _timestamp = DelayFirst ? DateTimeUtils.CurrentTimeMillis : (long?) null;
        }

    }

    public class PeriodicFiringControl : IFiringControl
    {

        private readonly object _lock = new object();

        private int _count;

        public PeriodicFiringControl(int period) => Period = period;

        public int Period { get; }

        public bool Check()
        {
            var flag = false;
            lock (_lock)
            {
                _count++;
                if (_count > Period)
                {
                    _count = 0;
                    flag = true;
                }
            }
            return flag;
        }

        public void Reset()
        {
            lock (_lock)
                _count = 0;
        }

    }

    public class SequentialFiringControl : IFiringControl
    {

        private readonly object _lock = new object();

        private readonly ISequence<bool> _bools;

        public SequentialFiringControl([NotNull] ISequence<bool> bools) => _bools = bools ?? throw new ArgumentNullException(nameof(bools));

        public bool Check()
        {
            lock (_lock)
                return _bools.Next();
        }

        public void Reset() { }

    }

    public class CountingFiringControl : IFiringControl
    {

        public static readonly CountingFiringControl NeverAllowed = new CountingFiringControl(0);

        public static readonly CountingFiringControl AlwaysAllowed = new CountingFiringControl(-1);

        private readonly ReaderWriterLockSlim _lock = LockUtils.RWLock();

        private int _count;

        public CountingFiringControl(int maxCount) => MaxFireCount = maxCount;

        public static CountingFiringControl Once() => new CountingFiringControl(1);

        public int MaxFireCount { get; }

        public int FiredCount
        {
            get
            {
                _lock.EnterReadLock();
                var count = _count;
                _lock.ExitReadLock();
                return count;
            }
        }

        public bool Check()
        {
            if (MaxFireCount < 0) return true;
            _lock.EnterWriteLock();
            var flag = _count < MaxFireCount;
            if (flag) _count++;
            _lock.ExitWriteLock();
            return flag;
        }

        public void Reset()
        {
            _lock.EnterWriteLock();
            _count = 0;
            _lock.ExitWriteLock();
        }

    }

}
