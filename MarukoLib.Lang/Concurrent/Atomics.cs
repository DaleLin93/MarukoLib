using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MarukoLib.Lang.Concurrent
{

    public interface IAtomic<T> 
    {

        /// <summary>
        /// Get value.
        /// </summary>
        /// <returns>Stored value</returns>
        T Get();

        /// <summary>
        /// Set value.
        /// </summary>
        /// <param name="value">New value</param>
        /// <returns>Old stored value</returns>
        T Set(T value);

        /// <summary>
        /// Set only if the <paramref name="oldValue"/> is equals to stored value.
        /// </summary>
        /// <param name="oldValue">Old value to compare</param>
        /// <param name="newValue">New value to set</param>
        /// <returns>Successfully set</returns>
        bool CompareAndSet(T oldValue, T newValue);

        /// <summary>
        /// Compute and set computed value.
        /// </summary>
        /// <param name="operator">Operator to compute new value.</param>
        /// <param name="oldValue">Stored value.</param>
        /// <param name="newValue">Computed value.</param>
        void Compute(UnaryOperator<T> @operator, out T oldValue, out T newValue);

    }

    public abstract class AbstractAtomic<T> : IAtomic<T>, IContainer, IContainer<T>
    {

        protected const bool UseSpinning = true;

        protected const int PreBlockSpinningCount = 15;

        private static readonly bool RefType = typeof(T).IsByRef;

        private readonly object _lock = new object();

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public abstract T Get();

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public abstract T Set(T value);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public abstract bool CompareAndSet(T oldValue, T newValue);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// Locking: optimistic locking (CAS, spinning count: <see cref="PreBlockSpinningCount"/>) -> pessimistic locking.
        /// </summary>
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public virtual void Compute(UnaryOperator<T> @operator, out T oldValue, out T newValue)
        {
            if (UseSpinning)
            {
                for (var i = 0; i < PreBlockSpinningCount; i++)
                {
                    newValue = @operator(oldValue = Get());
                    if (RefType ? ReferenceEquals(oldValue, newValue) : Equals(oldValue, newValue)) return;
                    if (CompareAndSet(oldValue, newValue)) return;
                }
            }
            lock (_lock) Set(newValue = @operator(oldValue = Get()));
        }

        object IContainer.Value => Get();

        T IContainer<T>.Value => Get();
        
    }

    public sealed class AtomicBool : AbstractAtomic<bool>
    {

        private int _val;

        public AtomicBool(bool val = default) => _val = val ? 1 : 0;

        /// <summary>
        /// Get or set value.
        /// </summary>
        public bool Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override bool Get() => Interlocked.CompareExchange(ref _val, 0, 0) == 1;

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override bool Set(bool value) => Interlocked.Exchange(ref _val, value ? 1 : 0) == 1;

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override bool CompareAndSet(bool oldValue, bool newValue) => 
            Interlocked.CompareExchange(ref _val, newValue ? 1 : 0, oldValue ? 1 : 0) == (oldValue ? 1 : 0);

        /// <summary>
        /// Set value to 'true', and returns true if the value was changed.
        /// </summary>
        /// <returns>Value changed</returns>
        public bool Set() => !Set(true);

        /// <summary>
        /// Set value to 'false', and returns true if the value was changed.
        /// </summary>
        /// <returns>Value changed</returns>
        public bool Reset() => Set(false);

    }

    public sealed class AtomicInt : AbstractAtomic<int>
    {

        private int _val;

        public AtomicInt(int val = default) => Interlocked.Exchange(ref _val, val);

        /// <summary>
        /// Get or set value.
        /// </summary>
        public int Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override int Get() => Interlocked.CompareExchange(ref _val, 0, 0);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override int Set(int value) => Interlocked.Exchange(ref _val, value);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override bool CompareAndSet(int oldValue, int newValue) => Interlocked.CompareExchange(ref _val, newValue, oldValue) == oldValue;

        public void Increment(int delta, out int oldValue, out int newValue) => Compute(v => v + delta, out oldValue, out newValue);

        public void Decrement(int delta, out int oldValue, out int newValue) => Compute(v => v - delta, out oldValue, out newValue);

        public int IncrementAndGet(int delta = 1)
        {
            Increment(delta, out _, out var val);
            return val;
        }

        public int GetAndIncrement(int delta = 1)
        {
            Increment(delta, out var val, out _);
            return val;
        }

        public int DecrementAndGet(int delta = 1)
        {
            Decrement(delta, out _, out var val);
            return val;
        }

        public int GetAndDecrement(int delta = 1)
        {
            Decrement(delta, out var val, out _);
            return val;
        }

    }

    public sealed class AtomicLong : AbstractAtomic<long>
    {

        private long _val;

        public AtomicLong(long val = default) => Interlocked.Exchange(ref _val, val);

        /// <summary>
        /// Get or set value.
        /// </summary>
        public long Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override long Get() => Interlocked.CompareExchange(ref _val, 0, 0);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override long Set(long value) => Interlocked.Exchange(ref _val, value);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override bool CompareAndSet(long oldValue, long newValue) => Interlocked.CompareExchange(ref _val, newValue, oldValue) == oldValue;

        public void Increment(long delta, out long oldValue, out long newValue) => Compute(v => v + delta, out oldValue, out newValue);

        public void Decrement(long delta, out long oldValue, out long newValue) => Compute(v => v - delta, out oldValue, out newValue);

        public long IncrementAndGet(long delta = 1)
        {
            Increment(delta, out _, out var val);
            return val;
        }

        public long GetAndIncrement(long delta = 1)
        {
            Increment(delta, out var val, out _);
            return val;
        }

        public long DecrementAndGet(long delta = 1)
        {
            Decrement(delta, out _, out var val);
            return val;
        }

        public long GetAndDecrement(long delta = 1)
        {
            Decrement(delta, out var val, out _);
            return val;
        }

    }

    public sealed class AtomicSingle : AbstractAtomic<float>
    {

        private float _val;

        public AtomicSingle(float val = default) => Interlocked.Exchange(ref _val, val);

        /// <summary>
        /// Get or set value.
        /// </summary>
        public float Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override float Get() => Interlocked.CompareExchange(ref _val, 0, 0);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override float Set(float value) => Interlocked.Exchange(ref _val, value);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public override bool CompareAndSet(float oldValue, float newValue) => Interlocked.CompareExchange(ref _val, newValue, oldValue) == oldValue;

        public void Increment(float delta, out float oldValue, out float newValue) => Compute(v => v + delta, out oldValue, out newValue);

        public void Decrement(float delta, out float oldValue, out float newValue) => Compute(v => v - delta, out oldValue, out newValue);

        public float IncrementAndGet(float delta = 1)
        {
            Increment(delta, out _, out var val);
            return val;
        }

        public float GetAndIncrement(float delta = 1)
        {
            Increment(delta, out var val, out _);
            return val;
        }

        public float DecrementAndGet(float delta = 1)
        {
            Decrement(delta, out _, out var val);
            return val;
        }

        public float GetAndDecrement(float delta = 1)
        {
            Decrement(delta, out var val, out _);
            return val;
        }

    }

    public sealed class AtomicDouble : AbstractAtomic<double>
    {

        private double _val;

        public AtomicDouble(double val = default) => Interlocked.Exchange(ref _val, val);

        /// <summary>
        /// Get or set value.
        /// </summary>
        public double Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override double Get() => Interlocked.CompareExchange(ref _val, 0, 0);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override double Set(double value) => Interlocked.Exchange(ref _val, value);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public override bool CompareAndSet(double oldValue, double newValue) => Interlocked.CompareExchange(ref _val, newValue, oldValue) == oldValue;

        public void Increment(double delta, out double oldValue, out double newValue) => Compute(v => v + delta, out oldValue, out newValue);

        public void Decrement(double delta, out double oldValue, out double newValue) => Compute(v => v - delta, out oldValue, out newValue);

        public double IncrementAndGet(double delta = 1)
        {
            Increment(delta, out _, out var val);
            return val;
        }

        public double GetAndIncrement(double delta = 1)
        {
            Increment(delta, out var val, out _);
            return val;
        }

        public double DecrementAndGet(double delta = 1)
        {
            Decrement(delta, out _, out var val);
            return val;
        }

        public double GetAndDecrement(double delta = 1)
        {
            Decrement(delta, out var val, out _);
            return val;
        }

    }

    public sealed class AtomicPtr : AbstractAtomic<IntPtr>
    {

        private IntPtr _val;

        public AtomicPtr(IntPtr val = default) => Interlocked.Exchange(ref _val, val);

        /// <summary>
        /// Get or set value.
        /// </summary>
        public IntPtr Value
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override IntPtr Get() => Interlocked.CompareExchange(ref _val, IntPtr.Zero, IntPtr.Zero);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override IntPtr Set(IntPtr value) => Interlocked.Exchange(ref _val, value);

        /// <summary>
        /// <inheritdoc cref="IAtomic{T}"/>
        /// </summary>
        public override bool CompareAndSet(IntPtr oldValue, IntPtr newValue) => Interlocked.CompareExchange(ref _val, newValue, oldValue) == oldValue;

    }

    public sealed class Atomic<T> : AbstractAtomic<T> where T : class
    {

        private T _ref;

        public Atomic(T @ref = null) => _ref = @ref;

        /// <summary>
        /// Get or set reference.
        /// </summary>
        public T Reference
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        /// Get reference.
        /// </summary>
        /// <returns>Stored reference</returns>
        public override T Get() => Interlocked.CompareExchange(ref _ref, null, null);

        /// <summary>
        /// Set reference.
        /// </summary>
        /// <param name="ref">New reference</param>
        /// <returns>Old stored reference</returns>
        public override T Set(T @ref) => Interlocked.Exchange(ref _ref, @ref);

        /// <summary>
        /// Set only if the <paramref name="oldRef"/> is equals to stored reference.
        /// </summary>
        /// <param name="oldRef">Old reference to compare</param>
        /// <param name="newRef">New reference to set</param>
        /// <returns>Successfully set</returns>
        public override bool CompareAndSet(T oldRef, T newRef) => ReferenceEquals(Interlocked.CompareExchange(ref _ref, newRef, oldRef), oldRef);

    }

}
