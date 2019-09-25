using System;
using System.Threading;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public enum TimeUnit : long
    {
        Day = TimeSpan.TicksPerDay,
        Hour = TimeSpan.TicksPerHour,
        Minute = TimeSpan.TicksPerMinute,
        Second = TimeSpan.TicksPerSecond,
        Millisecond = TimeSpan.TicksPerMillisecond,
        Tick = 1,
    }

    public static class TimeUnitExt
    {

        public static TimeSpan ToTimeSpan(this TimeUnit unit, long time) => new TimeSpan(ToTicks(unit, time));

        public static long ToDays(this TimeUnit unit, long time) => ConvertTo(unit, time, TimeUnit.Day);

        public static long ToHours(this TimeUnit unit, long time) => ConvertTo(unit, time, TimeUnit.Hour);

        public static long ToMinutes(this TimeUnit unit, long time) => ConvertTo(unit, time, TimeUnit.Minute);

        public static long ToSeconds(this TimeUnit unit, long time) => ConvertTo(unit, time, TimeUnit.Second);

        public static long ToMilliseconds(this TimeUnit unit, long time) => ConvertTo(unit, time, TimeUnit.Millisecond);

        public static long ToTicks(this TimeUnit unit, long time) => ConvertTo(unit, time, TimeUnit.Tick);

        public static long ConvertTo(this TimeUnit src, long time, TimeUnit dst) => src > dst ? time * ((long)src / (long)dst) : time / ((long)dst / (long)src);

        public static TimeSpan ToTimeSpan(this TimeUnit unit, double time) => new TimeSpan((long) ToTicks(unit, time));

        public static double ToDays(this TimeUnit unit, double time) => ConvertTo(unit, time, TimeUnit.Day);

        public static double ToHours(this TimeUnit unit, double time) => ConvertTo(unit, time, TimeUnit.Hour);

        public static double ToMinutes(this TimeUnit unit, double time) => ConvertTo(unit, time, TimeUnit.Minute);

        public static double ToSeconds(this TimeUnit unit, double time) => ConvertTo(unit, time, TimeUnit.Second);

        public static double ToMilliseconds(this TimeUnit unit, double time) => ConvertTo(unit, time, TimeUnit.Millisecond);

        public static double ToTicks(this TimeUnit unit, double time) => ConvertTo(unit, time, TimeUnit.Tick);

        public static double ConvertTo(this TimeUnit src, double time, TimeUnit dst) => src > dst ? time * ((double)src / (double)dst) : time / ((double)dst / (double)src);

    }

    public interface IClock
    {

        /// <summary>
        /// Used time unit of the clock.
        /// </summary>
        TimeUnit Unit { get; }

        /// <summary>
        /// Time value in specific time unit.
        /// The value must be monotone increasing by time.
        /// </summary>
        long Time { get; }
        
    }

    public abstract class Clock : IClock
    {

        private sealed class Delegated : Clock
        {

            private readonly Func<long> _func;

            public Delegated(Func<long> func, TimeUnit unit) : base(unit) => _func = func;

            public override long Time => _func();

        }

        public static readonly Clock SystemTicksClock = Of(() => DateTimeUtils.CurrentTimeTicks, TimeUnit.Tick);

        // ReSharper disable once InconsistentNaming
        public static readonly Clock TicksFromJan1st1970Clock = Of(() => DateTimeUtils.CurrentTimeTicks - DateTimeUtils.Jan1st1970Ticks, TimeUnit.Tick);

        public static readonly Clock SystemMillisClock = Of(() => DateTimeUtils.CurrentTimeMillis, TimeUnit.Millisecond);

        protected Clock(TimeUnit unit) => Unit = unit;

        public static Clock Of([NotNull] Func<long> func, TimeUnit unit) => new Delegated(func, unit);

        public TimeUnit Unit { get; }

        public abstract long Time { get; }

    }

    public abstract class OverridenClock : Clock
    {

        protected OverridenClock(IClock originalClock) : base(originalClock.Unit) => OriginalClock = originalClock;

        protected OverridenClock(IClock originalClock, TimeUnit unit) : base(unit) => OriginalClock = originalClock;

        public IClock OriginalClock { get; }

    }

    public class AlignedClock : OverridenClock
    {

        public AlignedClock(IClock originalClock, long offset) : base(originalClock) => Offset = offset;

        public static AlignedClock FromNow(IClock clock) => new AlignedClock(clock, -clock.Time); 

        public long Offset { get; }

        public override long Time => OriginalClock.Time + Offset;

    }

    public class FreezableClock : OverridenClock
    {

        public event EventHandler Frozen;

        public event EventHandler Unfrozen;

        private readonly ReaderWriterLockSlim _frozenLock = new ReaderWriterLockSlim();

        private bool _frozen;

        private long _frozenAt;

        public FreezableClock(IClock clock) : base(clock) { }

        public override long Time => IsFrozen ? _frozenAt : OriginalClock.Time - FrozenTime;

        public long FrozenTime { get; private set; }

        public bool IsFrozen
        {
            get
            {
                _frozenLock.EnterReadLock();
                try
                {
                    return _frozen;
                }
                finally
                {
                    _frozenLock.ExitReadLock();
                }
            }
        }

        public bool Freeze()
        {
            _frozenLock.EnterUpgradeableReadLock();
            try
            {
                if (_frozen) return false;
                _frozenLock.EnterWriteLock();
                try
                {
                    _frozen = true;
                    _frozenAt = OriginalClock.Time;
                }
                finally
                {
                    _frozenLock.ExitWriteLock();
                }
            }
            finally
            {
                _frozenLock.ExitUpgradeableReadLock();
            }
            Frozen?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool Unfreeze()
        {
            _frozenLock.EnterUpgradeableReadLock();
            try
            {
                if (!_frozen) return false;
                _frozenLock.EnterWriteLock();
                try
                {
                    _frozen = false;
                    FrozenTime += OriginalClock.Time - _frozenAt;
                }
                finally
                {
                    _frozenLock.ExitWriteLock();
                }
            }
            finally
            {
                _frozenLock.ExitUpgradeableReadLock();
            }
            Unfrozen?.Invoke(this, EventArgs.Empty);
            return true;
        }

    }

    public static class ClockExt
    {

        private class ConvertedClock : OverridenClock
        {

            public ConvertedClock(TimeUnit unit, IClock originalClock) : base(originalClock, unit) { }

            public override long Time => OriginalClock.Get(Unit);

        }

        private class MappedClock : OverridenClock
        {

            public MappedClock(IClock originalClock, UnaryOperator<long> @operator) : base(originalClock) => Operator = @operator;

            public UnaryOperator<long> Operator { get; }

            public override long Time => Operator(OriginalClock.Time);

        }

        public static IClock As(this IClock clock, TimeUnit timeUnit)
        {
            while (true)
            {
                if (clock.Unit == timeUnit) return clock;
                if (clock is ConvertedClock converted)
                {
                    clock = converted.OriginalClock;
                    continue;
                }
                return new ConvertedClock(timeUnit, clock);
            }
        }

        public static IClock Convert(this IClock clock, UnaryOperator<long> @operator) => new MappedClock(clock, @operator);

        public static IClock Adjust(this IClock clock, double scale) => Adjust(clock, clock.Time, scale);

        public static IClock Adjust(this IClock clock, long offset) => Convert(clock, time => time + offset);

        public static IClock Adjust(this IClock clock, long offset, double scale) => Convert(clock, time => (long)((time + offset) * scale));

        public static long GetDays(this IClock clock) => Get(clock, TimeUnit.Day);

        public static long GetHours(this IClock clock) => Get(clock, TimeUnit.Hour);

        public static long GetMinutes(this IClock clock) => Get(clock, TimeUnit.Minute);

        public static long GetSeconds(this IClock clock) => Get(clock, TimeUnit.Second);

        public static long GetMilliseconds(this IClock clock) => Get(clock, TimeUnit.Millisecond);

        public static long GetTicks(this IClock clock) => Get(clock, TimeUnit.Tick);

        public static long Get(this IClock clock, TimeUnit timeUnit) => clock.Unit.ConvertTo(clock.Time, timeUnit);

        public static TimeSpan GetTimeSpan(this IClock clock) => new TimeSpan(clock.Time * (long)clock.Unit);

    }

}
