namespace MarukoLib.Lang
{

    public struct Timestamped<T>
    {

        public Timestamped(long t, T v)
        {
            TimeStamp = t;
            Value = v;
        }

        public long TimeStamp { get; }

        public T Value { get; }

    }

}
