// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
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
    public class TestResponseChannelConsumer : IResponseChannelConsumer
    {
        private readonly StringBuilder _responseBuilder = new StringBuilder();
        
        public int CurrentExpectedResponseLength { get; set; }
        
        public IList<string> Responses { get; } = new List<string>();
        
        public AccessSafely UntilConsume { get; set; }
        
        public void Consume(IConsumerByteBuffer buffer)
        {
            var bytes = buffer.ToArray();
            buffer.Release();
            Consume(new ReadOnlySequence<byte>(bytes));
        }

        public void Consume(ReadOnlySequence<byte> buffer)
        {
            var responsePart = buffer.ToArray().BytesToText(0, (int)buffer.Length);
            _responseBuilder.Append(responsePart);
            
            if (_responseBuilder.Length >= CurrentExpectedResponseLength)
            {
                // assume currentExpectedRequestLength is length of all
                // requests when multiple are received at one time
                var combinedResponse = _responseBuilder.ToString();
                var combinedLength = combinedResponse.Length;
                _responseBuilder.Clear(); // reuse
      
                var startIndex = 0;
                var last = false;
                while (!last)
                {
                    var request = combinedResponse.Substring(startIndex, CurrentExpectedResponseLength);
                    startIndex += CurrentExpectedResponseLength;
        
                    Responses.Add(request);
        
                    last = startIndex == combinedLength;
                    UntilConsume.WriteUsing("clientConsume", 1);
                }
            }
        }
    }
}