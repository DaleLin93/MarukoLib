namespace MarukoLib.Lang
{

    public interface ITimestamped
    {

        long Timestamp { get; }

    }

    public struct Timestamped<T> : ITimestamped
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
