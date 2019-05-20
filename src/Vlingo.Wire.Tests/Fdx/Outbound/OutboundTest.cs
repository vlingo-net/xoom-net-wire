// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Vlingo.Wire.Message;
using Vlingo.Wire.Tests.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Outbound
{
    using Vlingo.Wire.Node;
    using Wire.Fdx.Outbound;
    
    public class OutboundTest : AbstractMessageTool
    {
        private static readonly string Message1 = "Message1";
        private static readonly string Message2 = "Message2";
        private static readonly string Message3 = "Message3";

        private MockManagedOutboundChannelProvider _channelProvider;
        private ByteBufferPool _pool;
        private Outbound _outbound;

        [Fact]
        public void TestBroadcast()
        {
            var rawMessage1 = RawMessage.From(0, 0, Message1);
            var rawMessage2 = RawMessage.From(0, 0, Message2);
            var rawMessage3 = RawMessage.From(0, 0, Message3);

            _outbound.Broadcast(rawMessage1);
            _outbound.Broadcast(rawMessage2);
            _outbound.Broadcast(rawMessage3);

            foreach (var channel in _channelProvider.AllOtherNodeChannels.Values)
            {
                var mock = (MockManagedOutboundChannel)channel;
                
                Assert.Equal(Message1, mock.Writes[0]);
                Assert.Equal(Message2, mock.Writes[1]);
                Assert.Equal(Message3, mock.Writes[2]);
            }
        }

        [Fact]
        public void TestBroadcastPooledByteBuffer()
        {
            var buffer1 = _pool.Access();
            var buffer2 = _pool.Access();
            var buffer3 = _pool.Access();
            
            var rawMessage1 = RawMessage.From(0, 0, Message1);
            rawMessage1.AsBuffer((MemoryStream)buffer1.AsStream());
            var rawMessage2 = RawMessage.From(0, 0, Message2);
            rawMessage2.AsBuffer((MemoryStream)buffer2.AsStream());
            var rawMessage3 = RawMessage.From(0, 0, Message3);
            rawMessage3.AsBuffer((MemoryStream)buffer3.AsStream());
            
            _outbound.Broadcast(buffer1);
            _outbound.Broadcast(buffer2);
            _outbound.Broadcast(buffer3);
            
            foreach (var channel in _channelProvider.AllOtherNodeChannels.Values)
            {
                var mock = (MockManagedOutboundChannel)channel;
                
                Assert.Equal(Message1, mock.Writes[0]);
                Assert.Equal(Message2, mock.Writes[1]);
                Assert.Equal(Message3, mock.Writes[2]);
            }
        }

        [Fact]
        public void TestBroadcastToSelectNodes()
        {
            var rawMessage1 = RawMessage.From(0, 0, Message1);
            var rawMessage2 = RawMessage.From(0, 0, Message2);
            var rawMessage3 = RawMessage.From(0, 0, Message3);

            var selectNodes = new List<Node> {Config.NodeMatching(Id.Of(3))};
            
            _outbound.Broadcast(selectNodes, rawMessage1);
            _outbound.Broadcast(selectNodes, rawMessage2);
            _outbound.Broadcast(selectNodes, rawMessage3);
            
            var mock = (MockManagedOutboundChannel) _channelProvider.ChannelFor(Id.Of(3));
            
            Assert.Equal(Message1, mock.Writes[0]);
            Assert.Equal(Message2, mock.Writes[1]);
            Assert.Equal(Message3, mock.Writes[2]);
        }

        [Fact]
        public void TestSendTo()
        {
            var rawMessage1 = RawMessage.From(0, 0, Message1);
            var rawMessage2 = RawMessage.From(0, 0, Message2);
            var rawMessage3 = RawMessage.From(0, 0, Message3);
            
            var id3 = Id.Of(3);
            
            _outbound.SendTo(rawMessage1, id3);
            _outbound.SendTo(rawMessage2, id3);
            _outbound.SendTo(rawMessage3, id3);
            
            var mock = (MockManagedOutboundChannel)_channelProvider.ChannelFor(Id.Of(3));
            
            Assert.Equal(Message1, mock.Writes[0]);
            Assert.Equal(Message2, mock.Writes[1]);
            Assert.Equal(Message3, mock.Writes[2]);
        }

        [Fact]
        public void TestSendToPooledByteBuffer()
        {
            var buffer1 = _pool.Access();
            var buffer2 = _pool.Access();
            var buffer3 = _pool.Access();
            
            var rawMessage1 = RawMessage.From(0, 0, Message1);
            rawMessage1.AsBuffer((MemoryStream)buffer1.AsStream());
            var rawMessage2 = RawMessage.From(0, 0, Message2);
            rawMessage2.AsBuffer((MemoryStream)buffer2.AsStream());
            var rawMessage3 = RawMessage.From(0, 0, Message3);
            rawMessage3.AsBuffer((MemoryStream)buffer3.AsStream());
            
            var id3 = Id.Of(3);
            
            _outbound.SendTo(buffer1, id3);
            _outbound.SendTo(buffer2, id3);
            _outbound.SendTo(buffer3, id3);
            
            var mock = (MockManagedOutboundChannel)_channelProvider.ChannelFor(Id.Of(3));
            
            Assert.Equal(Message1, mock.Writes[0]);
            Assert.Equal(Message2, mock.Writes[1]);
            Assert.Equal(Message3, mock.Writes[2]);
        }
        
        public OutboundTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            _pool = new ByteBufferPool(10, 1024);
            _channelProvider = new MockManagedOutboundChannelProvider(Id.Of(1), Config);
            _outbound = new Outbound(_channelProvider, new ByteBufferPool(10, 10_000));
        }
    }
}