// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace Vlingo.Wire.Message
{
    public class RawMessageHeader
    {
        private static readonly int ShortFields = 5;
        private static readonly int IntFields = 1;
        private static readonly int ShortBytes = sizeof(short) / sizeof(byte);
        private static readonly int IntBytes = sizeof(int) / sizeof(byte);
        private static readonly short HeaderId = 3730 | 0x01; // version 1

        public static int Bytes { get; } = ShortBytes * ShortFields + IntBytes * IntFields;

        private int _length;
        private short _nodeId;
        private short _type;

        public RawMessageHeader() : this(-1, -1, -1)
        {
        }
        
        public RawMessageHeader(int nodeId, int type, int length) : this((short)nodeId, (short)type, length)
        {
        }

        public RawMessageHeader(short nodeId, short type, int length)
        {
            _nodeId = nodeId;
            _type = type;
            _length = length;
        }

        public static RawMessageHeader From(Stream buffer)
        {
            var header = new RawMessageHeader();

            return header.Read(buffer);
        }

        public static RawMessageHeader From(short nodeId, short type, long length) => new RawMessageHeader(nodeId, type, (int) length);
        
        public static RawMessageHeader From(short nodeId, int type, long length) => new RawMessageHeader(nodeId, (short)type, (int) length);
        
        public static RawMessageHeader From(int nodeId, int type, long length) => new RawMessageHeader((short)nodeId, (short)type, (int) length);

        public static RawMessageHeader From(RawMessageHeader copy) => From(copy._nodeId, copy._type, copy._length);

        public int Length => _length;

        public short NodeId => _nodeId;

        public short Type => _type;

        public RawMessageHeader Read(Stream buffer)
        {
            using (var binaryReader = new BinaryReader(buffer, Converters.EncodingValue, true))
            {
                var headerId = binaryReader.ReadInt16();
                if (headerId != HeaderId)
                {
                    throw new ArgumentException($"Invalid raw message header: {headerId}. Expected: {HeaderId}");
                }

                var nodeId = binaryReader.ReadInt16();
                var type = binaryReader.ReadInt16();
                var length = binaryReader.ReadInt32();
                binaryReader.ReadInt16(); // unused1
                binaryReader.ReadInt16(); // unused2

                return SetAll(nodeId, type, length);
            }
        }

        public void CopyBytesTo(Stream buffer)
        {
            using (var binaryWriter = new BinaryWriter(buffer, Converters.EncodingValue, true))
            {
                binaryWriter.Write(HeaderId);
                binaryWriter.Write(_nodeId);
                binaryWriter.Write(_type);
                binaryWriter.Write(_length);
                binaryWriter.Write(short.MaxValue);
                binaryWriter.Write(short.MaxValue);
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(RawMessageHeader))
            {
                return false;
            }

            var otherHeader = (RawMessageHeader) obj;

            return
                _nodeId == otherHeader._nodeId &&
                _type == otherHeader._type &&
                _length == otherHeader._length;
        }

        public override int GetHashCode() => 31 * (_nodeId.GetHashCode() + _type.GetHashCode() + _length.GetHashCode());

        public override string ToString() => $"RawMessageHeader[headerId={HeaderId} nodeId={_nodeId} type={_type} length={_length}]";

        protected RawMessageHeader SetAll(short nodeId, short type, int length)
        {
            _nodeId = nodeId;
            _type = type;
            _length = length;

            return this;
        }
    }
}