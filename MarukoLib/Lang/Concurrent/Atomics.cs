using System.Threading;

namespace MarukoLib.Lang.Concurrent
{

    public sealed class AtomicBool
    {

        private int _val;

        public AtomicBool(bool val) => _val = val ? 1 : 0;

        /// <summary>
        /// Currently stored value.
        /// </summary>
        public bool Value => Interlocked.CompareExchange(ref _val, 0, 0) == 1;

        /// <summary>
        /// Set value.
        /// </summary>
        /// <param name="value">New value</param>
        /// <returns>Old value</returns>
        public bool Set(bool value) => Interlocked.Exchange(ref _val, value ? 1 : 0) == 1;

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

    public sealed class AtomicInt
    {

        private int _val;

        public AtomicInt(int val) => Interlocked.Exchange(ref _val, val);

        /// <summary>
        /// Currently stored value.
        /// </summary>
        public int Value => Interlocked.CompareExchange(ref _val, 0, 0);

        /// <summary>
        /// Set value.
        /// </summary>
        /// <param name="value">New value</param>
        /// <returns>Old value</returns>
        public int Set(int value) => Interlocked.Exchange(ref _val, value);

        /// <summary>
        /// Set if the old value is matched.
        /// </summary>
        /// <param name="oldValue">Old value to compare</param>
        /// <param name="newValue">New value to set</param>
        /// <returns>Successfully set</returns>
        public bool SetIf(int oldValue, int newValue) => Interlocked.CompareExchange(ref _val, newValue, oldValue) == oldValue;

    }

    public sealed class Atomic<T> where T : class
    {

        private T _val;

        public Atomic(T val = default) => _val = val;

        public T Get() => Interlocked.CompareExchange(ref _val, default, default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Old value</returns>
        public T Set(T value) => Interlocked.Exchange(ref _val, value);

    }

}
