// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.IO;

namespace Vlingo.Wire.Message
{
    public interface IConsumerByteBuffer
    {
        int Id { get; }
        
        void Release();
        string? Tag { get; set; }

        byte[] ToArray();
        
        int ArrayOffset { get; }
        
        bool HasArray { get; }

        // asByteBuffer
        // asCharBuffer
        // asShortBuffer
        // asIntBuffer
        // asLongBuffer
        // asFloatBuffer
        // asDoubleBuffer
        Stream AsStream();

        IConsumerByteBuffer Compact();
        
        long Capacity { get; }
        
        long Position();
        
        IConsumerByteBuffer Position(long newPosition);
        
        long Limit();
        
        IConsumerByteBuffer Limit(long newLimit);
        
        IConsumerByteBuffer Mark();
        
        IConsumerByteBuffer Reset();
        
        IConsumerByteBuffer Clear();
        
        IConsumerByteBuffer Flip();
        
        IConsumerByteBuffer Rewind();
        
        long Remaining { get; }
        
        bool HasRemaining { get; }
        
        bool IsReadOnly { get; }
        
        bool IsDirect { get; }

        byte Get();
        
        byte Get(int index);
        
        Stream Get(byte[] destination);
        
        Stream Get(byte[] destination, int offset, int length);
        
        char GetChar();
        
        char GetChar(int index);
        
        short GetShort();
        
        short GetShort(int index);
        
        int GetInt();
        
        int GetInt(int index);
        
        long GetLong();
        
        long GetLong(int index);
        
        float GetFloat();
        
        float GetFloat(int index);
        
        double GetDouble();
        
        double GetDouble(int index);

        IConsumerByteBuffer Put(Stream source);
        
        IConsumerByteBuffer Put(byte b);
        
        IConsumerByteBuffer Put(int index, byte b);
        
        IConsumerByteBuffer Put(byte[] src, int offset, int length);
        
        IConsumerByteBuffer Put(byte[] src);
        
        IConsumerByteBuffer PutChar(char value);
        
        IConsumerByteBuffer PutChar(int index, char value);
        
        IConsumerByteBuffer PutShort(short value);
        
        IConsumerByteBuffer PutShort(int index, short value);
        
        IConsumerByteBuffer PutInt(int value);
        
        IConsumerByteBuffer PutInt(int index, int value);
        
        IConsumerByteBuffer PutLong(long value);
        
        IConsumerByteBuffer PutLong(int index, long value);
        
        IConsumerByteBuffer PutFloat(float value);
        
        IConsumerByteBuffer PutFloat(int index, float value);
        
        IConsumerByteBuffer PutDouble(double value);
        
        IConsumerByteBuffer PutDouble(int index, double value);
    }
}