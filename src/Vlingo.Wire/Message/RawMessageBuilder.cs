// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Message
{
    public class RawMessageBuilder
    {
        private ScanMode _mode;
        private readonly RawMessage _rawMessage;
        private readonly MemoryStream _workBuffer;
      
        public RawMessageBuilder(int maxMessageSize)
        {
            _rawMessage = new RawMessage(maxMessageSize);
            _workBuffer = new MemoryStream(maxMessageSize);
            _mode = ScanMode.ReadHeader;
        }
      
        public RawMessage CurrentRawMessage()
        {
            if (IsCurrentMessageIncomplete())
            {
                throw new InvalidOperationException("The current raw message is incomplete.");
            }
        
            return _rawMessage;
        }
      
        public bool HasContent => _workBuffer.Position > 0;
      
        public bool IsCurrentMessageComplete()
        {
            var length = Length;
            var expected = _rawMessage.RequiredMessageLength;
      
            return length != 0 && length == expected;
        }
      
        public bool IsCurrentMessageIncomplete() => Length < _rawMessage.RequiredMessageLength;
      
        public long Length => _rawMessage.Length;
      
        public RawMessageBuilder PrepareContent()
        {
            _workBuffer.Flip();
            return this;
        }
      
        public RawMessageBuilder PrepareForNextMessage()
        {
            _rawMessage.Reset();
            return this;
        }
      
        public void Sync()
        {
            if (!Underflow())
            {
                var content = _workBuffer.ToArray();
                
                if (_mode == ScanMode.ReadHeader)
                {
                    _rawMessage.HeaderFrom(_workBuffer);
                }
                
                var messageTotalLength = _rawMessage.RequiredMessageLength;
                var missingRawMessageLength = messageTotalLength - _rawMessage.Length;
                var contentPosition = _workBuffer.Position;
                var availableContentLength = _workBuffer.Length - contentPosition;
            
                var appendLength = Math.Min(missingRawMessageLength, availableContentLength);
            
                _rawMessage.Append(content, contentPosition, appendLength);
                
                _workBuffer.Position = contentPosition + appendLength;
                
                if (availableContentLength == missingRawMessageLength)
                {
                  _workBuffer.Clear();
                  SetMode(ScanMode.ReadHeader);
                }
                else if (availableContentLength > missingRawMessageLength)
                {
                  SetMode(ScanMode.ReadHeader);
                }
                else if (availableContentLength < missingRawMessageLength)
                {
                  _workBuffer.Clear();
                  SetMode(ScanMode.ReuseHeader);
                }
            }
        }
      
        public Stream WorkBuffer() => _workBuffer;
      
        private void SetMode(ScanMode mode) => _mode = mode;
      
        private bool Underflow()
        {
            var remainingContentLength = _workBuffer.Length - _workBuffer.Position;
            var minimumRequiredLength = RawMessageHeader.Bytes + 1;
            
            if (_rawMessage.RequiredMessageLength == 0 && remainingContentLength < minimumRequiredLength)
            {
                var content = _workBuffer.ToArray();
                Array.Copy(content, _workBuffer.Position, content, 0, remainingContentLength);
                _workBuffer.Position = 0;
                _workBuffer.SetLength(remainingContentLength);
                SetMode(ScanMode.ReadHeader);
                return true;
            }
            
            return false;
        }
      
        private enum ScanMode
        {
            ReadHeader,
            ReuseHeader
        }
    }
}