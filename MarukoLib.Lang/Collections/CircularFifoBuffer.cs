using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MarukoLib.Lang.Collections
{

    public class CircularFifoBuffer<T> : ICollection<T>, IReadOnlyList<T>
    {

        private class Enumerator : IEnumerator<T>
        {

            private readonly CircularFifoBuffer<T> _buffer;

            private int _nextIndex;

            public Enumerator(CircularFifoBuffer<T> buffer)
            {
                _buffer = buffer;
                Reset();
            }

            public T Current { get; private set; }

            public bool MoveNext()
            {
                if (_nextIndex >= _buffer.Count) return false;
                Current = _buffer[_nextIndex++];
                return true;
            }

            public void Reset() => _nextIndex = 0;

            public void Dispose() { }

            object IEnumerator.Current => Current;

        }

        private readonly T[] _array;

        private long _tail, _head;

        public CircularFifoBuffer(long capacity)
        {
            Capacity = capacity;
            _array = new T[capacity];
            Clear();
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException($"count: {Count}, index: {index}");
                return _array[(_tail + index) % Capacity];
            }
            set
            {
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException($"count: {Count}, index: {index}");
                _array[(_tail + index) % Capacity] = value;
            }
        }

        public T First
        {
            get
            {
                if (Count <= 0) throw new InvalidOperationException("the collection is empty");
                return this[0];
            }
        }

        public T Last
        {
            get
            {
                var count = Count;
                if (count <= 0) throw new InvalidOperationException("the collection is empty");
                return this[count - 1];
            }
        }

        public long Capacity { get; }

        public int Count
        {
            get
            {
                var count = _head - _tail;
                return (int) (count < 0 ? count + Capacity : count);
            }
        }

        public bool IsFull => Count == Capacity;

        public bool IsReadOnly => false;

        public void Add(T val)
        {
            _array[_head] = val;
            _head = (_head + 1) % Capacity;
            if (_head == _tail)
                _tail = (_head + 1) % Capacity;
        }

        public bool Push(T val, out T pop)
        {
            var flag = IsFull;
            pop = flag ? _array[_head] : default;
            Add(val);
            return flag;
        }

        public T RemoveTail()
        {
            var t = default(T);
            var adjTail = _tail > _head ? _tail - Capacity : _tail;
            if (adjTail >= _head) return t;
            t = _array[_tail++];
            _tail %= Capacity;
            return t;
        }

        public void Clear()
        {
            _tail = 0;
            _head = 0;
        }

        public bool Contains(T item) => Enumerable.Contains(this, item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var v in this)
                array[arrayIndex++] = v;
        }

        public bool Remove(T item) => throw new NotSupportedException();

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        public override string ToString() => $"CircularFifoBuffer(capacity={Capacity}, head={_head}, tail={_tail})";

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
