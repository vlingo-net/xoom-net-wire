// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Actors.TestKit;
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
        public  void TestOutboundOperationsChannel()
        {
            var consumer = new MockChannelReaderConsumer();
            var consumeCount = 0;
            var accessSafely = AccessSafely.Immediately()
                .WritingWith<int>("consume", (value) => consumeCount += value)
                .ReadingWith("consume", () => consumeCount);
            consumer.UntilConsume = accessSafely;
            
            _opReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = OpMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _opChannel.Write(rawMessage1.AsStream(buffer));

            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 1, _opReader);
            
            Assert.Equal(1, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = OpMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _opChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 2, _opReader);
            
            Assert.Equal(2, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message2, consumer.Messages.Last());
        }
        
        [Fact]
        public void TestOutboundApplicationChannel()
        {
            var consumer = new MockChannelReaderConsumer();
            var consumeCount = 0;
            var accessSafely = AccessSafely.Immediately()
                .WritingWith<int>("consume", (value) => consumeCount += value)
                .ReadingWith("consume", () => consumeCount);
            consumer.UntilConsume = accessSafely;
            
            _appReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = AppMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _appChannel.Write(rawMessage1.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 1, _appReader);
            
            Assert.Equal(1, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = AppMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _appChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 2, _appReader);
            
            Assert.Equal(2, consumer.UntilConsume.ReadFrom<int>("consume"));
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
        
        private void ProbeUntilConsumed(Func<bool> reading, IChannelReader reader)
        {
            do
            {
                reader.ProbeChannel();
            } while (reading());
        }
    }
}