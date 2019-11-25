// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using Vlingo.Actors;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Fdx.Inbound;
using Vlingo.Wire.Node;
using Vlingo.Wire.Tests.Channel;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Inbound
{
    public class InboundStreamTest : IDisposable
    {
        private TestActor<IInboundStream> _inboundStream;
        private MockInboundStreamInterest _interest;
        private MockChannelReader _reader;
        private TestWorld _world;

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void TestInbound(int happenings)
        {
            var counter = 0;
            var accessSafely = AccessSafely.AfterCompleting(happenings)
                .WritingWith<int>("count", value => counter += value)
                .ReadingWith("count", () => counter);
            _interest.TestResult.UntilStops = accessSafely;
            _interest.TestResult.Happenings = happenings;

            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("count") < 1, _reader);

            _inboundStream.Actor.Stop();

            _interest.TestResult.UntilStops.ReadFromExpecting("count", happenings);
            
            var count = 0;
            var tempArray = _interest.TestResult.Messages.ToArray();

            for (var i = 1; i < _interest.TestResult.Messages.Count + 1; i++)
            {
                if (tempArray.Contains($"{MockChannelReader.MessagePrefix}{i}"))
                {
                    count++;
                }
            }
            
            Assert.True(_interest.TestResult.MessageCount.Get() > 0);
            Assert.Equal(count, _reader.ProbeChannelCount.Get());
        }

        public InboundStreamTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);

            _world = TestWorld.Start("test-inbound-stream");

            _interest = new MockInboundStreamInterest(output);

            _reader = new MockChannelReader();

            var definition = Definition.Has<InboundStreamActor>(Definition.Parameters(_interest, AddressType.Op, _reader, 10), "test-inbound");
            _inboundStream = _world.ActorFor<IInboundStream>(definition);
        }

        public void Dispose()
        {
            _world.Terminate();
        }

        private void ProbeUntilConsumed(Func<bool> reading, MockChannelReader reader)
        {
            do
            {
                reader.ProbeChannel();
            } while (reading());
        }
    }
}