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
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Inbound
{
    using Vlingo.Wire.Node;
    
    public class InboundSocketChannelTest: IDisposable
    {
        private static readonly string _appMessage = "APP TEST ";
        private static readonly string _opMessage = "OP TEST ";
        
        private ManagedOutboundSocketChannel _appChannel;
        private IChannelReader _appReader;
        private ManagedOutboundSocketChannel _opChannel;
        private IChannelReader _opReader;
        private Node _node;

        [Fact]
        public void TestOpInboundChannel()
        {
            /*var consumer = new MockChannelReaderConsumer();
            
            _opReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = _opMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _opChannel.Write(rawMessage1.AsStream(buffer));
            
            ProbeUntilConsumed(_opReader, consumer);
            
            Assert.Equal(1, consumer.ConsumeCount);
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = _opMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _opChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(_opReader, consumer);
            
            Assert.Equal(2, consumer.ConsumeCount);
            Assert.Equal(message2, consumer.Messages.Last());*/
        }

        [Fact(Skip = "freezing")]
        public void TestAppInboundChannel()
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
            
            var message1 = _appMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _appChannel.Write(rawMessage1.AsStream(buffer));

            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 1, _appReader);
            
            Assert.Equal(1, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = _appMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _appChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("consume") < 2, _appReader);
            
            Assert.Equal(2, consumer.UntilConsume.ReadFrom<int>("consume"));
            Assert.Equal(message2, consumer.Messages.Last());
        }

        public InboundSocketChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            _node = Node.With(Id.Of(2), Name.Of("node2"), Host.Of("localhost"), 37373, 37374);
            var logger = ConsoleLogger.TestInstance();
            _opChannel = new ManagedOutboundSocketChannel(_node, _node.OperationalAddress, logger);
            _appChannel = new ManagedOutboundSocketChannel(_node, _node.ApplicationAddress, logger);
            _opReader = new SocketChannelInboundReader(_node.OperationalAddress.Port, "test-op", 1024, logger);
            _appReader = new SocketChannelInboundReader(_node.ApplicationAddress.Port, "test-app", 1024, logger);
        }

        public void Dispose()
        {
            _appChannel.Dispose();
            _opChannel.Dispose();
            _appReader.Close();
            _opReader.Close();
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