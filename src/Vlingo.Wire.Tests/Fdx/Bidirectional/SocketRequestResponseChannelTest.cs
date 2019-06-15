// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
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
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional
{
    public class SocketRequestResponseChannelTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        
        private static readonly int PoolSize = 100;
        private static int TestPort = 37371;

        private MemoryStream _buffer;
        private ClientRequestResponseChannel _client;
        private TestResponseChannelConsumer _clientConsumer;
        private TestRequestChannelConsumerProvider _provider;
        private IServerRequestResponseChannel _server;
        private TestRequestChannelConsumer _serverConsumer;
        private World _world;

        [Fact]
        public async Task TestBasicRequestResponse()
        {
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length;
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            Request(request);
            
            _serverConsumer.UntilConsume = TestUntil.Happenings(1);
            _clientConsumer.UntilConsume = TestUntil.Happenings(1);

            while (_serverConsumer.UntilConsume.Remaining > 0)
            {
                await Task.Delay(1);
            }
            _serverConsumer.UntilConsume.Completes();
            
            while (_clientConsumer.UntilConsume.Remaining > 0)
            {
                _client.ProbeChannel();
            }
            _clientConsumer.UntilConsume.Completes();
            
            Assert.False(_serverConsumer.Requests.Count == 0);
        }

        [Fact]
        public async Task TestGappyRequestResponse()
        {
            var requestPart1 = "Request Part-1";
            var requestPart2 = "Request Part-2";
            var requestPart3 = "Request Part-3";

            _serverConsumer.CurrentExpectedRequestLength =
                requestPart1.Length + requestPart2.Length + requestPart3.Length;
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            
            // simulate network latency for parts of single request
            Request(requestPart1);
            await Task.Delay(100);
            Request(requestPart2);
            await Task.Delay(200);
            Request(requestPart3);
            _serverConsumer.UntilConsume = TestUntil.Happenings(1);
            while (_serverConsumer.UntilConsume.Remaining > 0)
            {
                ;
            }
            _serverConsumer.UntilConsume.Completes();
            
            _clientConsumer.UntilConsume = TestUntil.Happenings(1);
            while (_clientConsumer.UntilConsume.Remaining > 0)
            {
                await Task.Delay(1);
                _client.ProbeChannel();
            }
            _clientConsumer.UntilConsume.Completes();
            
            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(1, _serverConsumer.ConsumeCount);
            Assert.Equal(_serverConsumer.ConsumeCount, _serverConsumer.Requests.Count);
            
            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(1, _clientConsumer.ConsumeCount);
            Assert.Equal(_clientConsumer.ConsumeCount, _clientConsumer.Responses.Count);
            
            Assert.Equal(_clientConsumer.Responses[0], _serverConsumer.Requests[0]);
        }

        [Fact]
        public async Task Test10RequestResponse()
        {
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length + 1; // digits 0 - 9
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            
            _serverConsumer.UntilConsume = TestUntil.Happenings(10);
            _clientConsumer.UntilConsume = TestUntil.Happenings(10);
            
            for (var idx = 0; idx < 10; ++idx)
            {
                Request(request + idx);
            }

            while (_clientConsumer.UntilConsume.Remaining > 0)
            {
                await Task.Delay(1);
                _client.ProbeChannel();
            }

            _serverConsumer.UntilConsume.Completes();
            _clientConsumer.UntilConsume.Completes();
            
            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(10, _serverConsumer.ConsumeCount);
            Assert.Equal(_serverConsumer.ConsumeCount, _serverConsumer.Requests.Count);
    
            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(10, _clientConsumer.ConsumeCount);
            Assert.Equal(_clientConsumer.ConsumeCount, _clientConsumer.Responses.Count);
    
            for (int idx = 0; idx < 10; ++idx)
            {
                Assert.Equal(_clientConsumer.Responses[idx], _serverConsumer.Requests[idx]);
            }
        }
        
        [Fact]
        public async Task TestThatRequestResponsePoolLimitsNotExceeded()
        {
            var total = PoolSize * 2;
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length + 3; // digits 000 - 999
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
    
            _serverConsumer.UntilConsume = TestUntil.Happenings(total);
            _clientConsumer.UntilConsume = TestUntil.Happenings(total);
    
            for (int idx = 0; idx < total; ++idx)
            {
                Request(request + idx.ToString("D3"));
            }
            
            await Task.Delay(300);
    
            while (_clientConsumer.UntilConsume.Remaining > 0)
            {
                await Task.Delay(1);
                _client.ProbeChannel();
            }
            
            _serverConsumer.UntilConsume.Completes();
            _clientConsumer.UntilConsume.Completes();

            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(total, _serverConsumer.ConsumeCount);
            Assert.Equal(_serverConsumer.ConsumeCount, _serverConsumer.Requests.Count);
    
            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(total, _clientConsumer.ConsumeCount);
            Assert.Equal(_clientConsumer.ConsumeCount, _clientConsumer.Responses.Count);
    
            for (int idx = 0; idx < total; ++idx) 
            {
                Assert.Equal(_clientConsumer.Responses[idx], _serverConsumer.Requests[idx]);
            }
        }

        public SocketRequestResponseChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            
            _output = output;
            _world = World.StartWithDefault("test-request-response-channel");
            
            _buffer = new MemoryStream(1024);
            var logger = ConsoleLogger.TestInstance();
            _provider = new TestRequestChannelConsumerProvider();
            _serverConsumer = (TestRequestChannelConsumer)_provider.Consumer;

            _server = ServerRequestResponseChannelFactory.Start(
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
            try
            {
                Thread.Sleep(1000);
            }
            catch
            {
                // ignore
            }
            
            _server.Close();
            _client.Close();
            _buffer.Dispose();
            
            _world.Terminate();
        }
        
        private void Request(string request)
        {
            _buffer.Clear();
            _buffer.Write(Converters.TextToBytes(request));
            _buffer.Flip();
            _client.RequestWith(_buffer.ToArray());
        }
    }
}