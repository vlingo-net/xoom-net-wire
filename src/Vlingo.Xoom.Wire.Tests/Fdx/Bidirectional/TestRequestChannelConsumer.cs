// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Vlingo.Xoom.Actors.TestKit;
using Vlingo.Xoom.Common;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Message;

namespace Vlingo.Xoom.Wire.Tests.Fdx.Bidirectional;

public class TestRequestChannelConsumer : IRequestChannelConsumer
{
    private readonly StringBuilder _requestBuilder = new StringBuilder();
    private string _remaining = string.Empty;
        
    public int CurrentExpectedRequestLength { get; set; }

    public IList<string> Requests { get; } = new List<string>();
        
    public State CurrentState { get; set; }

    public void CloseWith(RequestResponseContext requestResponseContext, object data)
    {
    }

    public void Consume(RequestResponseContext context, IConsumerByteBuffer buffer)
    {
        var bytes = buffer.ToArray();
        buffer.Release();
        Consume(context,new ReadOnlySequence<byte>(bytes));
    }

    public void Consume(RequestResponseContext context, ReadOnlySequence<byte> buffer)
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
                if (startIndex > combinedLength || startIndex + CurrentExpectedRequestLength > combinedLength)
                {
                    _remaining = combinedRequests.Substring(currentIndex);
                    return;
                }

                var request = combinedRequests.Substring(startIndex, CurrentExpectedRequestLength);
                currentIndex += CurrentExpectedRequestLength;
                startIndex = startIndex + CurrentExpectedRequestLength;

                Requests.Add(request);
                CurrentState.Access.WriteUsing("consumeCount", 1);

                var responseBuffer = new BasicConsumerByteBuffer(1, CurrentExpectedRequestLength);
                context.RespondWith(responseBuffer.Clear().Put(Converters.TextToBytes(request)).Flip()); // echo back
        
                last = currentIndex == combinedLength;
            }
        }
    }
        
    public class State
    {
        public AccessSafely Access { get; private set; }

        private readonly AtomicInteger _consumeCount = new AtomicInteger(0);

        public State(int totalWrites) => Access = AfterCompleting(totalWrites);

        private AccessSafely AfterCompleting(int totalWrites)
        {
            Access = AccessSafely
                .AfterCompleting(totalWrites)
                .WritingWith<int>("consumeCount", _ => _consumeCount.Set(_consumeCount.IncrementAndGet()))
                .ReadingWith("consumeCount", _consumeCount.Get);
            return Access;
        }
    }
}