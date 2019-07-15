// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Actors;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Fdx.Inbound;
using Vlingo.Wire.Node;
using Vlingo.Wire.Tests.Channel;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Inbound
{
    public class InboundStreamTest: IDisposable
    {
        private TestActor<IInboundStream> _inboundStream;
        private MockInboundStreamInterest _interest;
        private MockChannelReader _reader;
        private TestWorld _world;

        [Fact]
        public void TestInbound()
        {
            _interest.TestResult.UntilStops = TestUntil.Happenings(1);
            while (_reader.ProbeChannelCount.Get() == 0)
                ;
            
            _inboundStream.Actor.Stop();
            
            int count = 0;
            foreach (var message in _interest.TestResult.Messages)
            {
                ++count;
                Assert.Equal(MockChannelReader.MessagePrefix + count, message);
            }
   
            _interest.TestResult.UntilStops.Completes();
    
            Assert.True(_interest.TestResult.MessageCount.Get() > 0);
            Assert.Equal(count, _reader.ProbeChannelCount.Get());
        }

        public InboundStreamTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            
            _world = TestWorld.Start("test-inbound-stream");
            
            _interest = new MockInboundStreamInterest();
            
            _reader = new MockChannelReader();
            
            var definition = Definition.Has<InboundStreamActor>(Definition.Parameters(_interest, AddressType.Op, _reader, 10),"test-inbound");
            
            _inboundStream = _world.ActorFor<IInboundStream>(definition);
        }

        public void Dispose()
        {
            _world.Terminate();
        }
    }
}