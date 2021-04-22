// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Actors.TestKit;
using Vlingo.Xoom.Common;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional
{
    public class TestSecureResponseChannelConsumer : IResponseChannelConsumer
    {
        private AccessSafely _access;
        private readonly List<string> _responses = new List<string>();

        public int CurrentExpectedResponseLength { get; set; }

        public IEnumerable<string> Responses => _responses;

        public AtomicInteger ConsumeCount { get; } = new AtomicInteger(0);
        
        public int TotalWrites => _access.TotalWrites;
        
        public void Consume(IConsumerByteBuffer buffer)
        {
            var responsePart = buffer.ToArray().BytesToText(0, (int)buffer.Limit());
            buffer.Release();
            _access.WriteUsing("responses", responsePart);
        }
        
        public int GetConsumeCount() => _access.ReadFrom<int>("consumeCount");

        public IEnumerable<string> GetResponses() => _access.ReadFrom<IEnumerable<string>>("responses");
        public AccessSafely AfterCompleting(int times)
        {
            _access = AccessSafely.AfterCompleting(times);

            _access.WritingWith<string>("responses", (response) => {
                _responses.Add(response);
                ConsumeCount.IncrementAndGet();
            });

            _access.ReadingWith<IEnumerable<string>>("responses", () => _responses);
            _access.ReadingWith("consumeCount", () => ConsumeCount.Get());

            return _access;
        }
    }
}