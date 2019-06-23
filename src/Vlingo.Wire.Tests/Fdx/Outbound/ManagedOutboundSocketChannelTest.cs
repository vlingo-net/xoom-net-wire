// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Inbound;
using Vlingo.Wire.Fdx.Outbound;
using Vlingo.Wire.Message;
using Vlingo.Wire.Tests.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Outbound
{
    using Vlingo.Wire.Node;
    
    public class ManagedOutboundSocketChannelTest : AbstractMessageTool, IDisposable
    {
        private static readonly string AppMessage = "APP TEST ";
        private static readonly string OpMessage = "OP TEST ";

        private ManagedOutboundSocketChannel _appChannel;
        private IChannelReader _appReader;
        private ManagedOutboundSocketChannel _opChannel;
        private IChannelReader _opReader;
        private Node _node;
        
        [Fact]
        public  async Task TestOutboundOperationsChannel()
        {
            var consumer = new MockChannelReaderConsumer();
            
            _opReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = OpMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _opChannel.Write(rawMessage1.AsStream(buffer));

            ProbeUntilConsumed(_opReader, consumer);
            
            Assert.Equal(1, consumer.ConsumeCount);
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = OpMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _opChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(_opReader, consumer);
            
            Assert.Equal(2, consumer.ConsumeCount);
            Assert.Equal(message2, consumer.Messages.Last());
        }
        
        [Fact]
        public void TestOutboundApplicationChannel()
        {
            var consumer = new MockChannelReaderConsumer();
            
            _appReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = AppMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _appChannel.Write(rawMessage1.AsStream(buffer));
            
            ProbeUntilConsumed(_appReader, consumer);
            
            Assert.Equal(1, consumer.ConsumeCount);
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = AppMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _appChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(_appReader, consumer);
            
            Assert.Equal(2, consumer.ConsumeCount);
            Assert.Equal(message2, consumer.Messages.Last());
        }

        public ManagedOutboundSocketChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            _node = Node.With(Id.Of(2), Name.Of("node2"), Host.Of("localhost"), 37375, 37376);
            var logger = ConsoleLogger.TestInstance();
            _opChannel = new ManagedOutboundSocketChannel(_node, _node.OperationalAddress, logger);
            _appChannel = new ManagedOutboundSocketChannel(_node, _node.ApplicationAddress, logger);
            _opReader = new SocketChannelInboundReader(_node.OperationalAddress.Port, "test-op", 1024, logger);
            _appReader = new SocketChannelInboundReader(_node.ApplicationAddress.Port, "test-app", 1024, logger);
        }

        public void Dispose()
        {
            _opChannel.Close();
            _appChannel.Close();
            _opReader.Close();
            _appReader.Close();
        }
        
        private void ProbeUntilConsumed(IChannelReader reader, MockChannelReaderConsumer consumer)
        {
            var currentConsumedCount = consumer.ConsumeCount;
    
            for (int idx = 0; idx < 100; ++idx)
            {
                reader.ProbeChannel();

                if (consumer.ConsumeCount > currentConsumedCount)
                {
                    break;
                }
            }
        }
    }
}