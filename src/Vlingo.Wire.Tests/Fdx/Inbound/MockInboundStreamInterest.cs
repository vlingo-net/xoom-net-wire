// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using System.Diagnostics;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Wire.Fdx.Inbound;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;
using Vlingo.Wire.Tests.Message;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Inbound
{
    public class MockInboundStreamInterest : AbstractMessageTool, IInboundStreamInterest
    {
        private readonly ITestOutputHelper _output;

        public MockInboundStreamInterest(ITestOutputHelper output)
        {
            _output = output;
            TestResult = new TestResults();
        }

        public readonly TestResults TestResult;

        public void HandleInboundStreamMessage(AddressType addressType, RawMessage message)
        {
            var textMessage = message.AsTextMessage();
            TestResult.Messages.Add(textMessage);
            TestResult.MessageCount.IncrementAndGet();
            _output.WriteLine($"INTEREST: {textMessage} list-size: {TestResult.Messages.Count} count: {TestResult.MessageCount.Get()} count-down: {TestResult.Happenings - TestResult.UntilStops.TotalWrites}");
            TestResult.UntilStops.WriteUsing("count", 1);            
        }

        public class TestResults
        {
            public readonly AtomicInteger MessageCount = new AtomicInteger(0);
            public readonly ConcurrentBag<string> Messages = new ConcurrentBag<string>();
            public AccessSafely UntilStops;
            public int Happenings;
        }
    }
}