// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Vlingo.Xoom.Wire.Channel;

namespace Vlingo.Xoom.Wire.Message
{
    /**
    * Reusable raw message with header. Assume one instance per client channel.
    * Thus, the header and the bytes are reused to avoid ridicules GC.
    */
    public class RawMessage
    {
        private readonly byte[] _bytes;
        private RawMessageHeader _header;
        private long _index;

        public RawMessage(RawMessageHeader header, long maxMessageSize)
        {
            _bytes = new byte[maxMessageSize];
            _header = header;
            _index = 0;
        }

        public RawMessage(long maxMessageSize): this(new RawMessageHeader(), maxMessageSize)
        {
        }

        public RawMessage(byte[] bytes)
        {
            _bytes = bytes;
            _header = new RawMessageHeader();
            _index = bytes.Length;
        }

        public RawMessage(RawMessage copy): this(copy.Length)
        {
            Append(copy._bytes, 0, copy.Length);
            Header(RawMessageHeader.From(copy._header));
        }
        
        public static RawMessage Copy(RawMessage original) => new RawMessage(original);

        public static RawMessage From(int nodeId, int type, int length) => new RawMessage(RawMessageHeader.From(nodeId, type, length), length);

        public static RawMessage From(int nodeId, int type, string textMessage)
        {
            var textBytes = Converters.TextToBytes(textMessage);
            var header = RawMessageHeader.From(nodeId, type, textBytes.Length);
            var message = new RawMessage(header, textBytes.Length);
            message.Append(textBytes, 0, textBytes.Length);
            return message;
        }

        public static RawMessage ReadFromWithHeader(Stream buffer)
        {
            var header = RawMessageHeader.From(buffer);
            var message = new RawMessage(header, header.Length);
            message.PutRemaining(buffer);
            return message;
        }

        public static RawMessage ReadFromWithoutHeader(Stream buffer)
        {
            var header = RawMessageHeader.From(0, 0, buffer.Length);
            var message = new RawMessage(header, header.Length);
            message.PutRemaining(buffer);
            return message;
        }

        public long Length => _index;

        public byte[] AsBinaryMessage => _bytes;

        public void Append(byte[] sourceBytes, long sourceIndex, long sourceLength)
        {
            Array.Copy(sourceBytes, sourceIndex, _bytes, _index, sourceLength);
            _index += sourceLength;
        }

        public RawMessageHeader Header() => _header;
        
        public void Header(RawMessageHeader header) => _header = header;

        public void HeaderFrom(Stream buffer) => _header.Read(buffer);

        public long RequiredMessageLength => _header.Length;
        
        public long TotalLength => RawMessageHeader.Bytes + Length;

        // Java version asByteBuffer
        public Stream AsStream() => AsStream(new MemoryStream(RawMessageHeader.Bytes + _bytes.Length));

        // Java version asByteBuffer
        public Stream AsStream(MemoryStream buffer)
        {
            buffer.Clear();
            CopyBytesTo(buffer);
            buffer.Flip();
            return buffer;
        }

        public byte[] AsBuffer(MemoryStream buffer)
        {
            var s = (MemoryStream)AsStream(buffer);
            return s.ToArray();
        }

        public string AsTextMessage() => Converters.BytesToText(_bytes, 0, (int)Length);

        public void CopyBytesTo(Stream buffer)
        {
            _header.CopyBytesTo(buffer);
            buffer.Write(_bytes, 0, _bytes.Length);
        }

        public RawMessage From(Stream buffer)
        {
            HeaderFrom(buffer);
            PutRemaining(buffer);
            return this;
        }

        public void Put(Stream buffer, bool flip)
        {
            if (flip)
            {
                buffer.Flip();
            }

            var length = buffer.Length;
            using (MemoryStream ms = new MemoryStream())
            {
                buffer.CopyTo(ms);
                Array.Copy(ms.ToArray(), 0, _bytes, _index, length);
            }

            _index = length;
        }

        public void Put(Stream buffer) => Put(buffer, true);

        public void PutRemaining(Stream buffer)
        {
            var length = buffer.Length - buffer.Position;
            using (MemoryStream ms = new MemoryStream())
            {
                // this copies from the actual position to the end of the stream so for the Array.Copy we need just to start at position = 0
                // because the call to ms.GetBuffer() will bring the remaining buffer.
                buffer.CopyTo(ms);
                Array.Copy(ms.GetBuffer(), 0, _bytes, _index, length);
            }

            _index = length;
        }

        public RawMessage Reset()
        {
            _index = 0;
            return this;
        }

        public override string ToString() => $"RawMessage[header={_header} length={Length}]";
    }
}