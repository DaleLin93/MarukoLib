using System.Threading;

namespace MarukoLib.Lang.Concurrent
{

    public class CyclicCounter
    {

        private readonly object _lock = new object();

        private readonly int _maxCount;

        private int _count;

        public CyclicCounter(int maxCount) => _maxCount = maxCount;

        public bool Count() => Count(out _);

        public bool Count(out int count)
        {
            lock (_lock)
            {
                count = ++_count;
                if (_count >= _maxCount)
                {
                    count = _count = 0;
                    return true;
                }
                return false;
            }
        }

        public bool Count(int expected)
        {
            Count(out var count);
            return count == expected;
        }

    }

}
