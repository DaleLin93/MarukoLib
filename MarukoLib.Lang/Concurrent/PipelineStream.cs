using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MarukoLib.Lang.Concurrent
{

    public class PipelineStream<T> : IDisposable
    {

        public readonly int Capacity;

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private readonly BlockingCollection<T> _queue;

        private volatile bool _closed;

        public PipelineStream(int capacity = -1)
        {
            Capacity = capacity;
            _queue = capacity < 1 ? new BlockingCollection<T>() : new BlockingCollection<T>(Capacity);
        }

        ~PipelineStream() => _queue.Dispose();

        public void Write(T value)
        {
            if (_closed) return;
            _queue.Add(value);
        }

        public int TryRead(out T value, int timeout)
        {
            value = default;
            if (_closed && _queue.IsEmpty()) return -1;
            if (timeout < 0) timeout = -1;
            try
            {
                return _queue.TryTake(out value, timeout, _tokenSource.Token) ? 1 : 0;
            }
            catch (OperationCanceledException)
            {
                return -1;
            }
        }

        public void Close()
        {
            _closed = true;
            _tokenSource.Cancel();
        }

        public void Dispose()
        {
            _queue.Dispose();
            _tokenSource.Dispose();
        }
    }

}
