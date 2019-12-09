using System;

namespace MarukoLib.Lang.Concurrent
{

    public abstract class FrequencyBarrier
    {

        public class MinimumInterval : FrequencyBarrier
        {

            private readonly object _sync = new object();

            private readonly Clock _clock;

            private readonly long _minimumInterval;

            private long? _last;

            public MinimumInterval(Clock clock, TimeSpan timeSpan)
                : this(clock, TimeUnit.Tick.ConvertTo(timeSpan.Ticks, clock.Unit)) { }

            public MinimumInterval(Clock clock, long minimumInterval)
            {
                _clock = clock;
                _minimumInterval = minimumInterval;
            }

            public override bool WaitOne(int millisecondsTimeout)
            {
                var timing = millisecondsTimeout > 0;
                var startTime = timing ? DateTimeUtils.CurrentTimeMillis : -1;
                do
                {
                    lock (_sync)
                    {
                        var time = _clock.Time;
                        if (_last == null || time - _last.Value >= _minimumInterval)
                        {
                            _last = time;
                            return true;
                        }
                    }
                } while (!timing || DateTimeUtils.CurrentTimeMillis - startTime < millisecondsTimeout);
                return false;
            }

        }

        public static FrequencyBarrier WithMinimumInterval(long intervalMillis) => new MinimumInterval(Clock.SystemMillisClock, intervalMillis);

        public abstract bool WaitOne(int millisecondsTimeout);

    }

    public static class FrequencyBarrierExt
    {

        public static bool WaitOne(this FrequencyBarrier frequencyBarrier) => frequencyBarrier.WaitOne(-1);

    }

}

