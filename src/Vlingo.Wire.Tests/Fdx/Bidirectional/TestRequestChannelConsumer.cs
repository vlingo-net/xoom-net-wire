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
    using System.Threading;

    public class TestRequestChannelConsumer : IRequestChannelConsumer
    {
        private int _count = 0;
        private readonly StringBuilder _requestBuilder = new StringBuilder();
        private string _remaining = string.Empty;
        
        public int CurrentExpectedRequestLength { get; set; }

        public IList<string> Requests { get; } = new List<string>();
        
        public AccessSafely UntilClosed { get; }
        
        public AccessSafely UntilConsume { get; set; }

        public ManualResetEvent Reset { get; set; }

        public void CloseWith<T>(RequestResponseContext<T> requestResponseContext, object data) => UntilClosed.WriteUsing("closed", 1);

        public void Consume<T>(RequestResponseContext<T> context, IConsumerByteBuffer buffer)
        {
            var bytes = buffer.ToArray();
            buffer.Release();
            Consume(context, new ReadOnlySequence<byte>(bytes));
        }

        public void Consume<T>(RequestResponseContext<T> context, ReadOnlySequence<byte> buffer)
        {
            var requestPart = buffer.ToArray().BytesToText(0, (int)buffer.Length);
            _requestBuilder.Append(_remaining).Append(requestPart);
            _remaining = string.Empty;
            if (_requestBuilder.Length >= CurrentExpectedRequestLength)
            {
                // assume currentExpectedRequestLength is length of all
                // requests when multiple are received at one time
                var combinedRequests = _requestBuilder.ToString();
                var combinedLength = combinedRequests.Length;
                _requestBuilder.Clear(); // reuse

                var startIndex = 0;
                var currentIndex = 0;
                var last = false;
                while (!last)
                {
                    if (startIndex > combinedRequests.Length)
                    {
                        _remaining = combinedRequests.Substring(currentIndex);
                        return;
                    }
                    
                    var request = combinedRequests.Substring(startIndex, CurrentExpectedRequestLength);
                    currentIndex += CurrentExpectedRequestLength;
                    startIndex = startIndex + CurrentExpectedRequestLength;

                    Requests.Add(request);

                    var responseBuffer = new BasicConsumerByteBuffer(1, CurrentExpectedRequestLength);
                    context.RespondWith(responseBuffer.Clear().Put(Converters.TextToBytes(request)).Flip()); // echo back
        
                    last = currentIndex == combinedLength;
                    _count++;

                    UntilConsume.WriteUsing("serverConsume", 1);
                }
            }
        }
    }
}