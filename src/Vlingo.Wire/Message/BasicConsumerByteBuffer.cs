// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.IO.Compression;

namespace Vlingo.Wire.Message
{
    public class BasicConsumerByteBuffer : IConsumerByteBuffer
    {
        private MemoryStream _buffer;
        private readonly int _id;
        private string _tag;
        private long _mark = 0;

        public BasicConsumerByteBuffer(int id, int maxBufferSize)
        {
            _id = id;
            _buffer = new MemoryStream(maxBufferSize);
        }
        
        public static BasicConsumerByteBuffer Allocate(int id, int maxBufferSize) => new BasicConsumerByteBuffer(id, maxBufferSize);

        public int Id => _id;

        public virtual void Release()
        {
        }

        public string Tag
        {
            get => _tag;
            set => _tag = value;
        }

        public byte[] Array() => _buffer.ToArray();

        // default values because ported from java this has no application here
        public int ArrayOffset => 0;

        // default values because ported from java this has no application here
        public bool HasArray => true;

        public Stream AsStream() => _buffer;

        public IConsumerByteBuffer Compact()
        {
            using(var compressStream = new MemoryStream())
            using(var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                _buffer.CopyTo(compressor);
                compressor.Close();
                _buffer = compressStream;
                return this;
            }
        }

        public long Capacity => _buffer.Capacity;

        public long Position() => _buffer.Position;

        public IConsumerByteBuffer Position(long newPosition)
        {
            if (newPosition > _buffer.Length || newPosition < 0)
            {
                throw new ArgumentException();
            }
            
            _buffer.Position = newPosition;
            
            if (_mark > _buffer.Position)
            {
                _mark = 0;
            }
            return this;
        }

        public long Limit() => _buffer.Length;

        public IConsumerByteBuffer Limit(long newLimit)
        {
            if (newLimit > _buffer.Capacity || newLimit < 0)
            {
                throw new ArgumentException();
            }
            
            _buffer.SetLength(newLimit);
            
            if (_buffer.Position > _buffer.Length)
            {
                _buffer.Position = _buffer.Length;
            }

            if (_mark > _buffer.Length)
            {
                _mark = 0;
            }
            
            return this;
        }

        public IConsumerByteBuffer Mark()
        {
            _mark = _buffer.Position;
            return this;
        }

        public IConsumerByteBuffer Reset()
        {
            var m = _mark;
            if (m < 0)
            {
                throw new Exception($"Invalid Mark exception : {m}");
            }
            _buffer.Position = m;
            return this;
        }

        public IConsumerByteBuffer Clear()
        {
            _buffer.SetLength(_buffer.Capacity);
            _buffer.Position = 0;
            _mark = 0;
            return this;
        }

        public IConsumerByteBuffer Flip()
        {
            _buffer.SetLength(_buffer.Position);
            _buffer.Position = 0;
            _mark = 0;
            return this;
        }

        public IConsumerByteBuffer Rewind()
        {
            _buffer.Position = 0;
            _mark = 0;
            return this;
        }

        public long Remaining => _buffer.Length - _buffer.Position;

        public bool HasRemaining => _buffer.Position < _buffer.Length;

        public bool IsReadOnly => !_buffer.CanWrite;

        // not sure about that one
        public bool IsDirect => false;

        public byte Get() => Convert.ToByte(_buffer.ReadByte());

        public byte Get(int index) => Convert.ToByte(ReadByteAt(index));

        public Stream Get(byte[] destination)
        {
            var destStream = new MemoryStream(destination);
            _buffer.CopyTo(destStream);
            return destStream;
        }

        public Stream Get(byte[] destination, int offset, int length)
        {
            _buffer.Read(destination, offset, length);
            return new MemoryStream(destination);
        }

        public char GetChar() => Convert.ToChar(_buffer.ReadByte());

        public char GetChar(int index) => Convert.ToChar(ReadByteAt(index));

        public short GetShort() => Convert.ToInt16(_buffer.ReadByte());

        public short GetShort(int index) => Convert.ToInt16(ReadByteAt(index));

        public int GetInt() => Convert.ToInt32(_buffer.ReadByte());

        public int GetInt(int index) => Convert.ToInt32(ReadByteAt(index));

        public long GetLong() => Convert.ToInt64(_buffer.ReadByte());

        public long GetLong(int index) => Convert.ToInt64(ReadByteAt(index));

        public float GetFloat() => Convert.ToSingle(_buffer.ReadByte());

        public float GetFloat(int index) => Convert.ToSingle(ReadByteAt(index));

        public double GetDouble() => Convert.ToDouble(_buffer.ReadByte());

        public double GetDouble(int index) => Convert.ToDouble(ReadByteAt(index));

        public IConsumerByteBuffer Put(Stream source)
        {   
            GuardReadOnly();
            
            var n = source.Length - source.Position;
            
            GuardOverflow(n);

            for (int i = 0; i < n; i++)
            {
                Put(Convert.ToByte(source.ReadByte()));
            }

            return this;
        }

        public IConsumerByteBuffer Put(byte b)
        {
            return PutValue(b);
        }

        public IConsumerByteBuffer Put(int index, byte b)
        {
            return PutValueIndex(index, b);
        }

        public IConsumerByteBuffer Put(byte[] src, int offset, int length)
        {
            GuardReadOnly();
            GuardOverflow(length);
            _buffer.Write(src, offset, length);
            return this;
        }

        public IConsumerByteBuffer Put(byte[] src)
        {
            GuardReadOnly();
            
            var n = src.Length;
            
            GuardOverflow(n);

            for (int i = 0; i < n; i++)
            {
                Put(src[i]);
            }

            return this;
        }

        public IConsumerByteBuffer PutChar(char value) => PutValue(Convert.ToByte(value));

        public IConsumerByteBuffer PutChar(int index, char value) => PutValueIndex(index, Convert.ToByte(value));

        public IConsumerByteBuffer PutShort(short value) => PutValue(Convert.ToByte(value));

        public IConsumerByteBuffer PutShort(int index, short value) => PutValueIndex(index, Convert.ToByte(value));

        public IConsumerByteBuffer PutInt(int value) => PutValue(Convert.ToByte(value));

        public IConsumerByteBuffer PutInt(int index, int value) => PutValueIndex(index, Convert.ToByte(value));

        public IConsumerByteBuffer PutLong(long value) => PutValue(Convert.ToByte(value));

        public IConsumerByteBuffer PutLong(int index, long value) => PutValueIndex(index, Convert.ToByte(value));

        public IConsumerByteBuffer PutFloat(float value) => PutValue(Convert.ToByte(value));

        public IConsumerByteBuffer PutFloat(int index, float value) => PutValueIndex(index, Convert.ToByte(value));

        public IConsumerByteBuffer PutDouble(double value) => PutValue(Convert.ToByte(value));

        public IConsumerByteBuffer PutDouble(int index, double value) => PutValueIndex(index, Convert.ToByte(value));

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(BasicConsumerByteBuffer))
            {
                return false;
            }

            return _id == ((BasicConsumerByteBuffer)obj)._id;
        }

        public override int GetHashCode() => 31 * _id.GetHashCode();

        public override string ToString() => $"BasicConsumerByteBuffer[id={_id}]";
        
        private int ReadByteAt(int index)
        {
            var buffer = new byte[1];
            return _buffer.Read(buffer, index, 1);
        }
        
        private IConsumerByteBuffer PutValue(byte b)
        {
            GuardReadOnly();
            GuardOverflow(1);
            _buffer.WriteByte(b);
            return this;
        }
        
        private IConsumerByteBuffer PutValueIndex(int index, byte b)
        {
            GuardReadOnly();
            _buffer.Write(new[] {b}, index, 1);
            return this;
        }

        private void GuardReadOnly()
        {
            if (IsReadOnly)
            {
                throw new Exception("Cannot write to ReadOnly buffer");
            }
        }
        
        private void GuardOverflow(long n)
        {
            if (n > Remaining)
            {
                throw new Exception("Cannot write to the buffer because it will overflow");
            }
        }
    }
}