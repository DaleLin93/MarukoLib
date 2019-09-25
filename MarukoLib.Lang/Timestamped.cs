namespace MarukoLib.Lang
{

    public struct Timestamped<T>
    {

        public Timestamped(long t, T v)
        {
            Timestamp = t;
            Value = v;
        }

        public long Timestamp { get; }

        public T Value { get; }

    }

}
