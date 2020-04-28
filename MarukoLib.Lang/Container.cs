using System;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public interface IContainer
    {

        bool IsReadOnly { get; }

        object Value { get; set; }

    }

    public interface IContainer<T>
    {

        bool IsReadOnly { get; }

        T Value { get; set; }

    }

    public sealed class Immutable<T> : IContainer<T>, IContainer
    {

        private readonly T _value;

        public Immutable(T value) => _value = value;

        public bool IsReadOnly => true;

        public T Value
        {
            get => _value;
            set => throw new NotSupportedException();
        }

        object IContainer.Value
        {
            get => _value;
            set => throw new NotSupportedException();
        }

    }

    public sealed class Mutable<T> : IContainer<T>, IContainer
    {

        public Mutable() {}

        public Mutable(T value) => Value = value;

        public bool IsReadOnly => false;

        public T Value { get; set; }

        object IContainer.Value
        {
            get => Value;
            set => Value = (T) value;
        }

    }
    
    public sealed class Memoized<T> : IContainer<T>, IContainer
    {

        [NotNull] private readonly IContainer<T> _container;

        [NotNull] private readonly Clock _clock;

        private readonly uint _expiration;

        private T _value;

        private long _expireAt;

        public Memoized([NotNull] IContainer<T> container, [NotNull] Clock clock, TimeSpan expiration) 
            : this(container, clock, (uint) TimeUnit.Tick.ConvertTo(expiration.Ticks, clock.Unit)) { }

        public Memoized([NotNull] IContainer<T> container, [NotNull] Clock clock, uint expiration)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _expiration = expiration;
            _expireAt = _clock.Time;
        }

        public bool IsReadOnly => _container.IsReadOnly;

        public T Value
        {
            get
            {
                var time = _clock.Time;
                if (time < _expireAt) return _value;
                _value = _container.Value;
                _expireAt = time + _expiration;
                return _value;
            }
            set
            {
                _container.Value = value;
                _value = value;
            }
        }

        object IContainer.Value
        {
            get => Value;
            set => Value = (T) value;
        }

    }

    public static class ContainerExt
    {

        public static IContainer<T> ToImmutable<T>(this IContainer<T> container) 
            => container is Immutable<T> immutable ? immutable : new Immutable<T>(container.Value);

    }

}
