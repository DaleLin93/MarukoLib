using System;
using JetBrains.Annotations;

namespace MarukoLib.Lang.Concurrent
{

    public abstract class FrequencyBarrier
    {

        public class MinimumInterval : FrequencyBarrier
        {

            private readonly IClock _clock;

            private readonly long _minimumInterval;

            private readonly IAtomic<long> _nextAt;

            public MinimumInterval([NotNull] IClock clock, TimeSpan timeSpan)
                : this(clock.As(TimeUnit.Tick), timeSpan.Ticks) { }

            public MinimumInterval([NotNull] IClock clock, long minimumInterval)
            {
                if (minimumInterval < 0) throw new ArgumentException($"Parameter '{nameof(minimumInterval)}' must be non-negative.");
                _clock = clock;
                _minimumInterval = minimumInterval;
                _nextAt = Atomics.Long(_clock.Time);
            }

            public override bool WaitOne(int millisecondsTimeout)
            {
                var instant = millisecondsTimeout == 0;
                var timing = millisecondsTimeout > 0;
                var startTime = timing ? DateTimeUtils.CurrentTimeMillis : -1;
                do
                {
                    var time = _clock.Time;
                    var nextAt = _nextAt.Get();
                    if (time >= nextAt && _nextAt.CompareAndSet(nextAt, time + _minimumInterval))
                        return true;
                } while (!instant && (!timing || DateTimeUtils.CurrentTimeMillis - startTime < millisecondsTimeout));
                return false;
            }

        }

        public static FrequencyBarrier MinIntervalSecs(double intervalSecs, IClock clock = null) 
            => new MinimumInterval(clock ?? Clocks.SystemMillisClock, TimeSpan.FromSeconds(intervalSecs));

        public abstract bool WaitOne(int millisecondsTimeout);

    }

    public static class FrequencyBarrierExt
    {

        public static bool GetOne(this FrequencyBarrier frequencyBarrier) => frequencyBarrier.WaitOne(0);

        public static bool WaitOne(this FrequencyBarrier frequencyBarrier) => frequencyBarrier.WaitOne(-1);

    }

}

