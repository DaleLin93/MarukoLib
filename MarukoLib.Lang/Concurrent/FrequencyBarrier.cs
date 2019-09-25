using System;

namespace MarukoLib.Lang.Concurrent
{

    public abstract class FrequencyBarrier
    {

        public class MinimumInterval : FrequencyBarrier
        {

            private readonly object _sync = new object();

            private readonly Clock _clock;

            private readonly uint _minimumInterval;

            private long? _last = null;

            public MinimumInterval(Clock clock, TimeSpan timeSpan)
                : this(clock, (uint)TimeUnit.Tick.ConvertTo(timeSpan.Ticks, clock.Unit)) { }

            public MinimumInterval(Clock clock, uint minimumInterval)
            {
                _clock = clock;
                _minimumInterval = minimumInterval;
            }

            public override bool WaitOne(int millisecondsTimeout)
            {
                var timing = millisecondsTimeout < 0;
                var startTime = timing ? DateTimeUtils.CurrentTimeMillis : -1;
                do
                {
                    lock (_sync)
                    {
                        if (_last == null || _clock.Time - _last.Value >= _minimumInterval)
                        {
                            _last = _clock.Time;
                            return true;
                        }
                    }
                } while (!timing || DateTimeUtils.CurrentTimeMillis - startTime < millisecondsTimeout);
                return false;
            }

        }

        public abstract bool WaitOne(int millisecondsTimeout);

    }

    public static class FrequencyBarrierExt
    {

        public static bool WaitOne(this FrequencyBarrier frequencyBarrier) => frequencyBarrier.WaitOne(-1);

    }

}

