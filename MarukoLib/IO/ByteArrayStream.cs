using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MarukoLib.IO
{

    [Serializable]
    public class ByteArrayStream : Stream
    {

        private readonly int _origin;

        private readonly bool _expandable;

        private int _position, _length, _capacity;

        private bool _isOpen;

        public ByteArrayStream() : this(0) { }

        public ByteArrayStream(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Buffer = new byte[capacity];
            _capacity = capacity;
            _expandable = true;
            CanWrite = true;
            _origin = 0;
            _isOpen = true;
        }

        public ByteArrayStream(byte[] buffer) : this(buffer, true) { }

        public ByteArrayStream(byte[] buffer, bool writable)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _length = _capacity = buffer.Length;
            CanWrite = writable;
            _origin = 0;
            _isOpen = true;
        }

        public ByteArrayStream(byte[] buffer, int index, int count) : this(buffer, index, count, true) { }

        public ByteArrayStream(byte[] buffer, int index, int count, bool writable)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - index < count) throw new ArgumentException("invalid buffer length");
            Buffer = buffer;
            _origin = _position = index;
            _length = _capacity = index + count;
            CanWrite = writable;
            _expandable = false;
            _isOpen = true;
        }

        public byte[] Buffer { get; private set; }

        public ArraySegment<byte> ArraySegment => new ArraySegment<byte>(Buffer, _origin, _length - _origin);

        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        /// <returns>
        /// <see langword="true" /> if the stream is open.</returns>
        public override bool CanRead => _isOpen;

        /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
        /// <returns>
        /// <see langword="true" /> if the stream is open.</returns>
        public override bool CanSeek => _isOpen;

        public override bool CanWrite { get; }

        public virtual int Capacity
        {
            get
            {
                if (!_isOpen) throw new IOException("Stream is closed");
                return _capacity - _origin;
            }
            set
            {
                if (value < Length) throw new ArgumentOutOfRangeException(nameof(value));
                if (!_isOpen) throw new IOException("Stream is closed");
                if (!_expandable && value != Capacity) throw new IOException("Stream is not expandable");
                if (!_expandable || value == _capacity) return;
                if (value > 0)
                {
                    var numArray = new byte[value];
                    if (_length > 0)
                        System.Buffer.BlockCopy(Buffer, 0, numArray, 0, _length);
                    Buffer = numArray;
                }
                else
                    Buffer = null;
                _capacity = value;
            }
        }

        /// <summary>Gets the length of the stream in bytes.</summary>
        /// <returns>The length of the stream in bytes.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed. </exception>
        public override long Length => _length - _origin;

        /// <summary>Gets or sets the current position within the stream.</summary>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The position is set to a negative value or a value greater than <see cref="F:System.Int32.MaxValue" />. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed. </exception>
        public override long Position
        {
            get => _position - _origin;
            set
            {
                if (value < 0L)
                    throw new ArgumentOutOfRangeException(nameof(value), "ArgumentOutOfRange_NeedNonNegNum");
                if (!_isOpen) throw new IOException("Stream is closed");
                if (value > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "ArgumentOutOfRange_StreamLength");
                _position = _origin + (int)value;
            }
        }

        public override void Flush() { }

        public void Reopen()
        {
            if (_isOpen) throw new IOException("Stream is opened");
            _isOpen = true;
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)  throw new ArgumentException("Argument_InvalidOffLen");
            if (!_isOpen) throw new IOException("Stream is closed");
            var byteCount = _length - _position;
            if (byteCount > count)
                byteCount = count;
            if (byteCount <= 0) return 0;
            if (byteCount <= 8)
            {
                var num = byteCount;
                while (--num >= 0)
                    buffer[offset + num] = Buffer[_position + num];
            }
            else
                System.Buffer.BlockCopy(Buffer, _position, buffer, offset, byteCount);
            _position += byteCount;
            return byteCount;
        }

        public override int ReadByte()
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            if (_position >= _length) return -1;
            return Buffer[_position++];
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            if (offset > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(offset), "ArgumentOutOfRange_StreamLength");
            switch (loc)
            {
                case SeekOrigin.Begin:
                    var num1 = _origin + (int)offset;
                    if (offset < 0L || num1 < _origin)
                        throw new IOException("IO.IO_SeekBeforeBegin");
                    _position = num1;
                    break;
                case SeekOrigin.Current:
                    var num2 = _position + (int)offset;
                    if (_position + offset < _origin || num2 < _origin)
                        throw new IOException("IO.IO_SeekBeforeBegin");
                    _position = num2;
                    break;
                case SeekOrigin.End:
                    var num3 = _length + (int)offset;
                    if (_length + offset < _origin || num3 < _origin)
                        throw new IOException("IO.IO_SeekBeforeBegin");
                    _position = num3;
                    break;
                default:
                    throw new ArgumentException("Argument_InvalidSeekOrigin");
            }
            return _position;
        }

        /// <summary>Sets the length of the current stream to the specified value.</summary>
        /// <param name="value">The value at which to set the length. </param>
        /// <exception cref="T:System.NotSupportedException">The current stream is not resizable and <paramref name="value" /> is larger than the current capacity.-or- The current stream does not support writing. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="value" /> is negative or is greater than the maximum length of the <see cref="T:System.IO.MemoryStream" />, where the maximum length is(<see cref="F:System.Int32.MaxValue" /> - origin), and origin is the index into the underlying buffer at which the stream starts. </exception>
        public override void SetLength(long value)
        {
            if (value < 0L || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "ArgumentOutOfRange_StreamLength");
            EnsureWriteable();
            if (value > int.MaxValue - _origin)
                throw new ArgumentOutOfRangeException(nameof(value), "ArgumentOutOfRange_StreamLength");
            var num = _origin + (int)value;
            if (!EnsureCapacity(num) && num > _length)
                Array.Clear(Buffer, _length, num - _length);
            _length = num;
            if (_position <= num)
                return;
            _position = num;
        }

        /// <summary>Writes the stream contents to a byte array, regardless of the <see cref="P:System.IO.MemoryStream.Position" /> property.</summary>
        /// <returns>A new byte array.</returns>
        public virtual byte[] ToArray()
        {
            var numArray = new byte[_length - _origin];
            System.Buffer.BlockCopy(Buffer, _origin, numArray, 0, _length - _origin);
            return numArray;
        }

        /// <summary>Writes a block of bytes to the current stream using data read from a buffer.</summary>
        /// <param name="buffer">The buffer to write data from. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The maximum number of bytes to write. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing. For additional information see <see cref="P:System.IO.Stream.CanWrite" />.-or- The current position is closer than <paramref name="count" /> bytes to the end of the stream, and the capacity cannot be modified. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="offset" /> subtracted from the buffer length is less than <paramref name="count" />. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="offset" /> or <paramref name="count" /> are negative. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The current stream instance is closed. </exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Argument_InvalidOffLen");
            if (!_isOpen) throw new IOException("Stream is closed");
            EnsureWriteable();
            var num1 = _position + count;
            if (num1 < 0)
                throw new IOException("IO.IO_StreamTooLong");
            if (num1 > _length)
            {
                var flag = _position > _length;
                if (num1 > _capacity && EnsureCapacity(num1))
                    flag = false;
                if (flag)
                    Array.Clear(Buffer, _length, num1 - _length);
                _length = num1;
            }
            if (count <= 8 && buffer != Buffer)
            {
                var num2 = count;
                while (--num2 >= 0)
                    Buffer[_position + num2] = buffer[offset + num2];
            }
            else
                System.Buffer.BlockCopy(buffer, offset, Buffer, _position, count);
            _position = num1;
        }

        /// <summary>Writes a byte to the current stream at the current position.</summary>
        /// <param name="value">The byte to write. </param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support writing. For additional information see <see cref="P:System.IO.Stream.CanWrite" />.-or- The current position is at the end of the stream, and the capacity cannot be modified. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The current stream is closed. </exception>
        public override void WriteByte(byte value)
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            EnsureWriteable();
            if (_position >= _length)
            {
                var num = _position + 1;
                var flag = _position > _length;
                if (num >= _capacity && EnsureCapacity(num))
                    flag = false;
                if (flag)
                    Array.Clear(Buffer, _length, _position - _length);
                _length = num;
            }
            Buffer[_position++] = value;
        }

        /// <summary>Writes the entire contents of this memory stream to another stream.</summary>
        /// <param name="stream">The stream to write this memory stream to. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="stream" /> is <see langword="null" />. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The current or target stream is closed. </exception>
        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "ArgumentNull_Stream");
            if (!_isOpen) throw new IOException("Stream is closed");
            stream.Write(Buffer, _origin, _length - _origin);
        }

        internal void InternalGetOriginAndLength(out int origin, out int length)
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            origin = _origin;
            length = _length;
        }

        internal int InternalGetPosition()
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            return _position;
        }

        internal int InternalReadInt32()
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            var num = _position += 4;
            if (num > _length)
            {
                _position = _length;
                throw new EndOfStreamException("end of file");
            }
            return Buffer[num - 4] | Buffer[num - 3] << 8 | Buffer[num - 2] << 16 | Buffer[num - 1] << 24;
        }

        internal int InternalEmulateRead(int count)
        {
            if (!_isOpen) throw new IOException("Stream is closed");
            var num = _length - _position;
            if (num > count)
                num = count;
            if (num < 0)
                num = 0;
            _position += num;
            return num;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            _isOpen = false;
        }

        private void EnsureWriteable()
        {
            if (CanWrite) return;
            throw new IOException("Write not supported");
        }

        private bool EnsureCapacity(int value)
        {
            if (value < 0) throw new IOException("Stream too long");
            if (value <= _capacity) return false;
            var num = value;
            if (num < 256)
                num = 256;
            if (num < _capacity * 2)
                num = _capacity * 2;
            if ((uint)(_capacity * 2) > 2147483591U)
                num = value > 2147483591 ? value : 2147483591;
            Capacity = num;
            return true;
        }

    }
}
