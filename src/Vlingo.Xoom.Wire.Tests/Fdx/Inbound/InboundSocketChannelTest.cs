// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vlingo.Xoom.Actors.Plugin.Logging.Console;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Fdx.Inbound;
using Vlingo.Xoom.Wire.Fdx.Outbound;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Xoom.Wire.Tests.Fdx.Inbound
{
    public class InboundSocketChannelTest: IDisposable
    {
        private const string AppMessage = "APP TEST ";
        private const string OpMessage = "OP TEST ";

        private static int _testPort = 37673;

        private readonly ManagedOutboundSocketChannel _appChannel;
        private readonly IChannelReader _appReader;
        private readonly ManagedOutboundSocketChannel _opChannel;
        private readonly IChannelReader _opReader;

        [Fact]
        public void TestOpInboundChannel()
        {
            var consumer = new MockChannelReaderConsumer();
            var accessSafely = consumer.AfterCompleting(2);
            
            _opReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = OpMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _opChannel.Write(rawMessage1.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFromNow<int>("count") < 1, _opReader, 10);
            
            Assert.Equal(1, accessSafely.ReadFromNow<int>("count"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = OpMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _opChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFromNow<int>("count") < 2, _opReader, 10);
            
            Assert.Equal(2, accessSafely.ReadFromNow<int>("count"));
            Assert.Equal(message2, consumer.Messages.Last());
        }

        [Fact]
        public void TestAppInboundChannel()
        {
            var consumer = new MockChannelReaderConsumer();
            var accessSafely = consumer.AfterCompleting(2);

            _appReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = AppMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            _appChannel.Write(rawMessage1.AsStream(buffer));

            ProbeUntilConsumed(() => accessSafely.ReadFromNow<int>("count") < 1, _appReader, 10);
            
            Assert.Equal(1, accessSafely.ReadFromNow<int>("count"));
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = AppMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            _appChannel.Write(rawMessage2.AsStream(buffer));
            
            ProbeUntilConsumed(() => accessSafely.ReadFromNow<int>("count") < 2, _appReader, 10);
            
            Assert.Equal(2, accessSafely.ReadFromNow<int>("count"));
            Assert.Equal(message2, consumer.Messages.Last());
        }

        public InboundSocketChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            var node = Node.With(Id.Of(2), Name.Of("node2"), Host.Of("localhost"), _testPort, _testPort + 1);
            var logger = ConsoleLogger.TestInstance();
            _opChannel = new ManagedOutboundSocketChannel(node, node.OperationalAddress, logger);
            _appChannel = new ManagedOutboundSocketChannel(node, node.ApplicationAddress, logger);
            _opReader = new SocketChannelInboundReader(node.OperationalAddress.Port, "test-op", 1024, logger);
            _appReader = new SocketChannelInboundReader(node.ApplicationAddress.Port, "test-app", 1024, logger);
            ++_testPort;
        }

        public void Dispose()
        {
            _appChannel.Dispose();
            _opChannel.Dispose();
            _appReader.Close();
            _opReader.Close();
        }

        private void ProbeUntilConsumed(Func<bool> reading, IChannelReader reader, int maxSeconds)
        {
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                reader.ProbeChannel();
            } while (reading() || sw.Elapsed.TotalSeconds >= maxSeconds);
            sw.Stop();
        }
    }
}