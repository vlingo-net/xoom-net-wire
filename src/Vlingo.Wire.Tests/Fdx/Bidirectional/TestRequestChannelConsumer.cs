// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional
{
    public class TestRequestChannelConsumer : IRequestChannelConsumer
    {
        private int _currentExpectedRequestLength;
        private int _consumeCount;
        private readonly IList<string> _requests = new List<string>();
        private TestUntil _untilClosed;
        private TestUntil _untilConsume;

        private readonly StringBuilder _requestBuilder = new StringBuilder();
        private string _remaining = string.Empty;
        
        public void CloseWith<T>(RequestResponseContext<T> requestResponseContext, object data) => _untilClosed?.Happened();

        public void Consume<T>(RequestResponseContext<T> context, IConsumerByteBuffer buffer)
        {
            var bytes = buffer.Array();
            buffer.Release();
            Consume(context, new ReadOnlySequence<byte>(bytes));
        }

        public void Consume<T>(RequestResponseContext<T> context, ReadOnlySequence<byte> buffer)
        {
            var requestPart = buffer.ToArray().BytesToText(0, (int)buffer.Length);
            _requestBuilder.Append(_remaining).Append(requestPart);
            _remaining = string.Empty;
            if (_requestBuilder.Length >= _currentExpectedRequestLength)
            {
                // assume currentExpectedRequestLength is length of all
                // requests when multiple are received at one time
                var combinedRequests = _requestBuilder.ToString();
                var combinedLength = combinedRequests.Length;
                _requestBuilder.Clear(); // reuse

                var currentIndex = 0;
                var last = false;
                while (!last)
                {
                    var endIndex = currentIndex + _currentExpectedRequestLength;
                    if (endIndex > combinedRequests.Length)
                    {
                        _remaining = combinedRequests.Substring(currentIndex);
                        return;
                    }
                    
                    var request = combinedRequests.Substring(currentIndex, endIndex);
                    currentIndex += _currentExpectedRequestLength;
                    _requests.Add(request);
                    ++_consumeCount;
                    
                    var responseBuffer = new BasicConsumerByteBuffer(1, _currentExpectedRequestLength);
                    context.RespondWith(responseBuffer.Clear().Put(Converters.TextToBytes(request)).Flip()); // echo back
        
                    last = currentIndex == combinedLength;

                    _untilConsume?.Happened();
                }
            }
        }
    }
}