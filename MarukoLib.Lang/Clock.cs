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
        Tick = 1
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

        /// <summary>
        /// The speed of the clock corresponding to the real world.
        /// </summary>
        decimal Speed { get; }

    }

    public abstract class Clock : IClock
    {

        protected Clock(TimeUnit unit) => Unit = unit;

        public TimeUnit Unit { get; }

        public abstract long Time { get; }

        public abstract decimal Speed { get; }

    }

    public sealed class UtcTickClock : Clock
    {

        public static readonly UtcTickClock FromJan1st1970 = new UtcTickClock(-DateTimeUtils.Jan1st1970UtcTicks);

        public UtcTickClock(long offset = 0) : base(TimeUnit.Tick) => Offset = offset;

        public long Offset { get; }

        public override long Time => DateTime.UtcNow.Ticks + Offset;

        public override decimal Speed => 1;
    } 

    public sealed class DelegatedClock : Clock
    {

        private readonly Supplier<long> _supplier;

        public DelegatedClock(Supplier<long> supplier, TimeUnit unit, decimal speed) : base(unit)
        {
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
            Speed = speed;
        }

        public override long Time => _supplier();

        public override decimal Speed { get; }

    }

    public class OverridenClock : Clock
    {

        public OverridenClock([NotNull] IClock originalClock, TimeUnit unit) : base(unit) 
            => OriginalClock = originalClock ?? throw new ArgumentNullException(nameof(originalClock));

        protected OverridenClock([NotNull] IClock originalClock) : this(originalClock, originalClock.Unit) { }

        [NotNull] public IClock OriginalClock { get; }

        public override long Time => Unit == OriginalClock.Unit ? OriginalClock.Time : OriginalClock.Get(Unit);

        public override decimal Speed => OriginalClock.Speed;

    }

    public class AlignedClock : OverridenClock
    {

        public AlignedClock([NotNull] IClock originalClock, long offset) : base(originalClock) => Offset = offset;

        public static AlignedClock FromNow([NotNull] IClock clock) => new AlignedClock(clock, -clock.Time); 

        public long Offset { get; }

        public override long Time => OriginalClock.Time + Offset;

    }

    public class TransformedClock : OverridenClock
    {

        public TransformedClock([NotNull] IClock originalClock, [NotNull] UnaryOperator<long> op) : base(originalClock) 
            => Operator = op ?? throw new ArgumentNullException(nameof(op));

        [NotNull] public UnaryOperator<long> Operator { get; }

        public override long Time => Operator(OriginalClock.Time);

    }

    public class SyncedClock : OverridenClock
    {

        private readonly long _localBase, _remoteBase;

        private readonly double _multiplier;

        public SyncedClock([NotNull] IClock originalClock, long localTime, long remoteTime, double multiplier = 1) : base(originalClock)
        {
            if (multiplier < 0) throw new ArgumentException($"{nameof(multiplier)} cannot be negative");
            _localBase = localTime;
            _remoteBase = remoteTime;
            _multiplier = multiplier;
        }

        public static SyncedClock Sync([NotNull] IClock clock, long remoteTime, double multiplier = 1) => new SyncedClock(clock, clock.Time, remoteTime, multiplier);

        public static bool TrySync([NotNull] IClock clock, Supplier<long> remoteTimeSupplier, TimeSpan syncWithin, int maxRetryCount, out SyncedClock synced) 
            => TrySync(clock, unit => remoteTimeSupplier(), syncWithin, maxRetryCount, out synced);

        public static bool TrySync([NotNull] IClock clock, Func<TimeUnit, long> remoteTimeSupplier, TimeSpan syncWithin, int maxRetryCount, out SyncedClock synced)
        {
            for (var i = maxRetryCount - 1; i >= 0; i--)
            {
                var startTicks = clock.GetTicks();
                var remoteTime = remoteTimeSupplier(clock.Unit);
                var endTicks = clock.GetTicks();
                var elapsedTicks = endTicks - startTicks;
                if (elapsedTicks / 2 < syncWithin.Ticks)
                {
                    synced = new SyncedClock(clock, TimeUnit.Tick.ConvertTo(startTicks, clock.Unit), remoteTime, 1);
                    return true;
                }
            }
            synced = null;
            return false;
        }

        public override long Time => _remoteBase + (long)((OriginalClock.Time - _localBase) * _multiplier);

    }

    /// <summary>
    /// The freezable clock is a clock that can be paused and resumed.
    /// </summary>
    public class FreezableClock : OverridenClock
    {

        public event EventHandler Frozen;

        public event EventHandler Unfrozen;

        private readonly ReaderWriterLockSlim _frozenLock = new ReaderWriterLockSlim();

        private bool _frozen;

        private long _frozenAt;

        public FreezableClock([NotNull] IClock clock) : base(clock) { }

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

    public static class Clocks
    {

        public static readonly IClock SystemTicksClock = UtcTickClock.FromJan1st1970;

        public static readonly IClock SystemMillisClock = new OverridenClock(SystemTicksClock, TimeUnit.Millisecond);

        public static IClock Of([NotNull] Supplier<long> supplier, TimeUnit unit, decimal speed = 1) => new DelegatedClock(supplier, unit, speed);

    }

    public static class ClockExt
    {

        public static IClock As([NotNull] this IClock clock, TimeUnit timeUnit)
        {
            for (; ; )
            {
                if (clock.Unit == timeUnit) return clock;
                if (clock.GetType() == typeof(OverridenClock))
                {
                    clock = ((OverridenClock)clock).OriginalClock;
                    continue;
                }
                return new OverridenClock(clock, timeUnit);
            }
        }

        public static IClock Convert([NotNull] this IClock clock, [NotNull] UnaryOperator<long> @operator) => new TransformedClock(clock, @operator);

        public static IClock Adjust([NotNull] this IClock clock, double scale) => Adjust(clock, clock.Time, scale);

        public static IClock Adjust([NotNull] this IClock clock, long offset) => Convert(clock, time => time + offset);

        public static IClock Adjust([NotNull] this IClock clock, long offset, double scale)
        {
            if (scale < 0) throw new ArgumentException($"{nameof(scale)} cannot be negative");
            return Convert(clock, time => (long)((time + offset) * scale));
        }

        public static long GetDays([NotNull] this IClock clock) => Get(clock, TimeUnit.Day);

        public static long GetHours([NotNull] this IClock clock) => Get(clock, TimeUnit.Hour);

        public static long GetMinutes([NotNull] this IClock clock) => Get(clock, TimeUnit.Minute);

        public static long GetSeconds([NotNull] this IClock clock) => Get(clock, TimeUnit.Second);

        public static long GetMilliseconds([NotNull] this IClock clock) => Get(clock, TimeUnit.Millisecond);

        public static long GetTicks([NotNull] this IClock clock) => Get(clock, TimeUnit.Tick);

        public static long Get([NotNull] this IClock clock, TimeUnit unit)
        {
            while (clock.GetType() == typeof(OverridenClock))
                clock = ((OverridenClock)clock).OriginalClock;
            return clock.Unit.ConvertTo(clock.Time, unit);
        }

        public static TimeSpan GetTimeSpan([NotNull] this IClock clock) => new TimeSpan(clock.Time * (long)clock.Unit);

    }

}
