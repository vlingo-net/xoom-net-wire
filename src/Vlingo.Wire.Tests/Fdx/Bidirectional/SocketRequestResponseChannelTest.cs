// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Bidirectional;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;
using Xunit;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional
{
    public class SocketRequestResponseChannelTest : IDisposable
    {
        private static readonly int PoolSize = 100;
        private static int TestPort = 37371;

        private MemoryStream _buffer;
        private ClientRequestResponseChannel _client;
        private TestResponseChannelConsumer _clientConsumer;
        private TestRequestChannelConsumerProvider _provider;
        private IServerRequestResponseChannel _server;
        private TestRequestChannelConsumer _serverConsumer;
        private World _world;

        [Fact(Skip = "Not working because of some issues with Proxy generation")]
        public async Task TestBasicRequestResponse()
        {
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length;
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            await Request(request);
            
            _serverConsumer.UntilConsume = TestUntil.Happenings(1);
            _clientConsumer.UntilConsume = TestUntil.Happenings(1);

            while (_serverConsumer.UntilConsume.Remaining > 0)
            {
                await Task.Delay(1);
            }
            _serverConsumer.UntilConsume.Completes();
            
            while (_clientConsumer.UntilConsume.Remaining > 0)
            {
                await _client.ProbeChannelAsync();
            }
            _clientConsumer.UntilConsume.Completes();
            
            Assert.False(_serverConsumer.Requests.Count == 0);
        }

        public SocketRequestResponseChannelTest()
        {
            _world = World.StartWithDefault("test-request-response-channel");
            
            _buffer = new MemoryStream(1024);
            var logger = ConsoleLogger.TestInstance();
            _provider = new TestRequestChannelConsumerProvider();
            _serverConsumer = (TestRequestChannelConsumer)_provider.Consumer;

            _server = ServerRequestResponseChannel.Start(
                _world.Stage,
                _provider,
                TestPort,
                "test-server",
                1,
                PoolSize,
                10240,
                10L);
            
            _clientConsumer = new TestResponseChannelConsumer();
            
            _client = new ClientRequestResponseChannel(Address.From(Host.Of("localhost"), TestPort, AddressType.None),
                _clientConsumer, PoolSize, 10240, logger);

            ++TestPort;
        }

        public void Dispose()
        {
            _server.Close();
            _client.Close();
            _buffer.Dispose();

            try
            {
                Thread.Sleep(1000);
            }
            catch
            {
                // ignore
            }
            
            _world.Terminate();
        }
        
        private async Task Request(string request)
        {
            _buffer.Clear();
            _buffer.Write(Converters.TextToBytes(request));
            _buffer.Flip();
            await _client.RequestWithAsync(_buffer);
        }
    }
}