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

            public override void Acquire()
            {
                lock (_sync)
                {
                    while (!(_last == null || _clock.Time - _last.Value >= _minimumInterval)) { }
                    _last = _clock.Time;
                }
            }

        }

        public abstract void Acquire();

    }

}

