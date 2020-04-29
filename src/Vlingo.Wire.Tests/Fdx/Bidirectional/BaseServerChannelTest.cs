// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Bidirectional;
using Vlingo.Wire.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional
{
    public abstract class BaseServerChannelTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly int _poolSize;

        private readonly MemoryStream _buffer;
        private readonly IClientRequestResponseChannel _client;
        private readonly TestResponseChannelConsumer _clientConsumer;
        private readonly IServerRequestResponseChannel _server;
        private readonly TestRequestChannelConsumer _serverConsumer;
        private readonly World _world;

        [Fact]
        public void TestBasicRequestResponse()
        {
            _output.WriteLine("Starting TestBasicRequestResponse");
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length;
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            _serverConsumer.CurrentState = new TestRequestChannelConsumer.State(1);
            _clientConsumer.CurrentState = new TestResponseChannelConsumer.State(1);
            
            Request(request);

            var remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            while (remaining != 0)
            {
                _client.ProbeChannel();
                remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            }

            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(1, _serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _serverConsumer.Requests.Count);

            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(1, _clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _clientConsumer.Responses.Count);

            Assert.Equal(_clientConsumer.Responses[0], _serverConsumer.Requests[0]);
        }

        [Fact]
        public void TestGappyRequestResponse()
        {
            _output.WriteLine("Starting TestGappyRequestResponse");
            var requestPart1 = "Request Part-1";
            var requestPart2 = "Request Part-2";
            var requestPart3 = "Request Part-3";

            _serverConsumer.CurrentExpectedRequestLength =
                requestPart1.Length + requestPart2.Length + requestPart3.Length;
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            
            _serverConsumer.CurrentState = new TestRequestChannelConsumer.State(1);
            _clientConsumer.CurrentState = new TestResponseChannelConsumer.State(1);
            
            // simulate network latency for parts of single request
            Request(requestPart1);
            Thread.Sleep(100);
            Request(requestPart2);
            Thread.Sleep(200);
            Request(requestPart3);
            
            var remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            while (remaining != 0)
            {
                _client.ProbeChannel();
                remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            }
            
            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(1, _serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _serverConsumer.Requests.Count);

            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(1, _clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _clientConsumer.Responses.Count);

            Assert.Equal(_clientConsumer.Responses[0], _serverConsumer.Requests[0]);
        }

        [Fact]
        public void Test10RequestResponse()
        {
            _output.WriteLine("Starting Test10RequestResponse");
            var total = 10;
            var request = "Hello, Request-Response";

            _serverConsumer.CurrentExpectedRequestLength = request.Length + 1; // digits 0 - 9
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;
            
            _serverConsumer.CurrentState = new TestRequestChannelConsumer.State(total);
            _clientConsumer.CurrentState = new TestResponseChannelConsumer.State(total);

            for (var idx = 0; idx < total; ++idx)
            {
                Request(request + idx);
            }

            var remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            while (remaining != 0)
            {
                _client.ProbeChannel();
                remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            }
            
            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(total, _serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _serverConsumer.Requests.Count);

            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(total, _clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _clientConsumer.Responses.Count);

            Assert.Equal(_clientConsumer.Responses[0], _serverConsumer.Requests[0]);
    
            for (var idx = 0; idx < total; ++idx)
            {
                Assert.Equal(_clientConsumer.Responses[idx], _serverConsumer.Requests[idx]);
            }
        }
        
        [Fact]
        public void TestThatRequestResponsePoolLimitsNotExceeded()
        {
            _output.WriteLine("Starting TestThatRequestResponsePoolLimitsNotExceeded");
            var total = _poolSize * 2;
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length + 3; // digits 000 - 999
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;

            _serverConsumer.CurrentState = new TestRequestChannelConsumer.State(total);
            _clientConsumer.CurrentState = new TestResponseChannelConsumer.State(total);
    
            for (var idx = 0; idx < total; ++idx)
            {
                Request(request + idx.ToString("D3"));
            }
            
            var remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            while (remaining != 0)
            {
                _client.ProbeChannel();
                remaining = _clientConsumer.CurrentState.Access.ReadFromNow<int>("remaining");
            }

            Assert.True(_serverConsumer.Requests.Any());
            Assert.Equal(total, _serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_serverConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _serverConsumer.Requests.Count);

            Assert.True(_clientConsumer.Responses.Any());
            Assert.Equal(total, _clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"));
            Assert.Equal(_clientConsumer.CurrentState.Access.ReadFrom<int>("consumeCount"), _clientConsumer.Responses.Count);

            Assert.Equal(_clientConsumer.Responses[0], _serverConsumer.Requests[0]);
    
            for (var idx = 0; idx < total; ++idx)
            {
                Assert.Equal(_clientConsumer.Responses[idx], _serverConsumer.Requests[idx]);
            }
        }

        public BaseServerChannelTest(ITestOutputHelper output, int poolSize)
        {
            _output = output;
            _poolSize = poolSize;
            var converter = new Converter(output);
            Console.SetOut(converter);

            _world = World.StartWithDefaults("test-request-response-channel");
            
            _buffer = new MemoryStream(1024);
            var logger = ConsoleLogger.TestInstance();
            var provider = new TestRequestChannelConsumerProvider();
            _serverConsumer = (TestRequestChannelConsumer)provider.Consumer;

            _server = GetServer(
                _world.Stage,
                provider,
                "test-server",
                1,
                _poolSize,
                10240,
                10L,
                1L);
            
            _clientConsumer = new TestResponseChannelConsumer();
            
            _client = GetClient(_clientConsumer, _poolSize, 10240, logger);
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

        protected abstract IClientRequestResponseChannel GetClient(
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            ILogger logger);

        protected abstract IServerRequestResponseChannel GetServer(
            Stage stage,
            IRequestChannelConsumerProvider provider,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout);
        
        private void Request(string request)
        {
            _buffer.Clear();
            _buffer.Write(Converters.TextToBytes(request));
            _buffer.Flip();
            _client.RequestWith(_buffer.ToArray());
        }
    }
}