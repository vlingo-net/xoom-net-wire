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

public class TestResponseChannelConsumer : IResponseChannelConsumer
{
    private readonly StringBuilder _responseBuilder = new StringBuilder();

    public int CurrentExpectedResponseLength { get; set; }

    public IList<string> Responses { get; } = new List<string>();

    public State CurrentState { get; set; }

    public void Consume(IConsumerByteBuffer buffer)
    {
        var bytes = buffer.ToArray();
        buffer.Release();
        Consume(new ReadOnlySequence<byte>(bytes));
    }

    public void Consume(ReadOnlySequence<byte> buffer)
    {
        var responsePart = buffer.ToArray().BytesToText(0, (int) buffer.Length);
        _responseBuilder.Append(responsePart);

        if (_responseBuilder.Length >= CurrentExpectedResponseLength)
        {
            // assume currentExpectedRequestLength is length of all
            // requests when multiple are received at one time
            var combinedResponse = _responseBuilder.ToString();
            var combinedLength = combinedResponse.Length;
                
            var startIndex = 0;
            var last = false;
            while (!last)
            {
                var request = combinedResponse.Substring(startIndex, CurrentExpectedResponseLength);
                startIndex += CurrentExpectedResponseLength;

                Responses.Add(request);
                CurrentState.Access.WriteUsing("consumeCount", 1);

                _responseBuilder.Clear(); // reuse
                if (startIndex + CurrentExpectedResponseLength > combinedLength)
                {
                    //Received combined responses has a part of a response.
                    // Should save the part and append to the next combined responses.
                    last = true;
                    _responseBuilder.Append(combinedResponse, startIndex, combinedLength - startIndex);   
                       
                }
                else
                {
                    last = startIndex == combinedLength;
                }
            }
        }
    }

    public class State
    {
        public AccessSafely Access { get; private set; }

        private readonly AtomicInteger _consumeCount = new AtomicInteger(0);
        private readonly AtomicInteger _remaining;

        public State(int totalWrites)
        {
            _remaining = new AtomicInteger(totalWrites);
            Access = AfterCompleting(totalWrites);
        }

        private AccessSafely AfterCompleting(int totalWrites)
        {
            Access = AccessSafely
                .AfterCompleting(totalWrites)
                .WritingWith("consumeCount", Increment)
                .ReadingWith("consumeCount", _consumeCount.Get)
                .ReadingWith("remaining", _remaining.Get);
            return Access;
        }

        private void Increment()
        {
            _consumeCount.IncrementAndGet();
            _remaining.DecrementAndGet();
        }
    }
}