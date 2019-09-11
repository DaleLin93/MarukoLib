using System;
using System.Collections;
using System.Collections.Generic;

namespace MarukoLib.Lang.Collections
{

    internal class Enumerator<T> : IEnumerator<T>
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
            if (_nextIndex >= _buffer.Count)
                return false;
            Current = _buffer[_nextIndex++];
            return true;
        }

        public void Reset() => _nextIndex = 0;

        public void Dispose() { }

        object IEnumerator.Current => Current;

    }

    public class CircularFifoBuffer<T> : ICollection<T>
    {
        private readonly T[] _array;

        private int _tail;

        private int _head;

        public CircularFifoBuffer(int capacity)
        {
            Capacity = capacity;
            _array = new T[capacity];
            Clear();
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                return _array[(_tail + index) % Capacity];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                _array[(_tail + index) % Capacity] = value;
            }
        }

        public int Capacity { get; }

        public int Count
        {
            get
            {
                var count = _head - _tail;
                return count < 0 ? count + Capacity : count;
            }
        }

        public bool IsReadOnly => false;

        public void Add(T val)
        {
            _array[_head] = val;
            _head = (_head + 1) % Capacity;
            if (_head == _tail)
                _tail = (_head + 1) % Capacity;
        }

        public T RemoveTail()
        {
            var t = default(T);
            var adjTail = _tail > _head ? _tail - Capacity : _tail;
            if (adjTail >= _head) return t;
            t = (T)_array[_tail++];
            _tail = _tail % Capacity;
            return t;
        }

        public void Clear()
        {
            _tail = 0;
            _head = 0;
        }

        public bool Contains(T item)
        {
            foreach (var v in this)
                if (Equals(v, item))
                    return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var v in this)
                array[arrayIndex++] = v;
        }

        public bool Remove(T item) => throw new NotSupportedException();

        public IEnumerator<T> GetEnumerator() => new Enumerator<T>(this);

        public override string ToString() => $"CircularFifoBuffer(capacity={Capacity}, head={_head}, tail={_tail})";

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

}
