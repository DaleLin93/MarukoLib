using System;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{
    public interface IContainer
    {

        object Value { get; }

    }

    public interface IContainer<out T>
    {

        T Value { get; }

    }

    public sealed class Immutable<T> : IContainer, IContainer<T>
    {

        public Immutable(T value) => Value = value;

        public T Value { get; }

        object IContainer.Value => Value;

    }

    public sealed class Mutable<T> : IContainer, IContainer<T>
    {

        public Mutable() {}

        public Mutable(T value) => Value = value;

        public T Value { get; set; }

        object IContainer.Value => Value;

    }

    public sealed class Memoized<T> : IContainer, IContainer<T>
    {

        private readonly Func<T> _supplier;

        private readonly Clock _clock;

        private readonly uint? _expiration;

        private T _value = default;

        private long? _valueTimestamp = null;

        public Memoized([NotNull] Func<T> supplier)
        {
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
            _clock = null;
            _expiration = null;
        }

        public Memoized([NotNull] Func<T> supplier, [NotNull] Clock clock, TimeSpan timeSpan) 
            : this(supplier, clock, (uint) TimeUnit.Tick.ConvertTo(timeSpan.Ticks, clock.Unit)) { }

        public Memoized([NotNull] Func<T> supplier, [NotNull] Clock clock, uint expiration)
        {
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _expiration = expiration;
        }

        public T Value
        {
            get
            {
                var time = _clock?.Time ?? 0;
                if (_valueTimestamp == null || _expiration != null && time > _valueTimestamp + _expiration.Value)
                {
                    _value = _supplier();
                    _valueTimestamp = time;
                }
                return _value;
            }
        }

        object IContainer.Value => Value;

    }
}
