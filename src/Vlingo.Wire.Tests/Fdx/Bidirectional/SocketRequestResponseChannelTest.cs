// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading;
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
        private static int _testPort = 37471;

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

            var serverConsumeCount = 0;
            var clientConsumeCount = 0;
            var accessSafely = AccessSafely.AfterCompleting(1)
                .WritingWith<int>("serverConsume", (value) => serverConsumeCount += value)
                .ReadingWith("serverConsume", () => serverConsumeCount)
                .WritingWith<int>("clientConsume", (value) => clientConsumeCount += value)
                .ReadingWith("clientConsume", () => clientConsumeCount);
            _serverConsumer.UntilConsume = accessSafely;
            _clientConsumer.UntilConsume = accessSafely;

            Request(request);

            while (_serverConsumer.UntilConsume.ReadFrom<int>("serverConsume") < 1)
            {
                Thread.Sleep(1);
            }
            _serverConsumer.UntilConsume.ReadFromExpecting("serverConsume", 1);
            
            while (_clientConsumer.UntilConsume.ReadFrom<int>("clientConsume") < 1)
            {
                _client.ProbeChannel();
            }
            _clientConsumer.UntilConsume.ReadFromExpecting("clientConsume", 1);
            
            Assert.False(_serverConsumer.Requests.Count == 0);
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
            
            var serverConsumeCount = 0;
            var clientConsumeCount = 0;
            var accessSafely = AccessSafely.AfterCompleting(1)
                .WritingWith<int>("serverConsume", (value) => serverConsumeCount += value)
                .ReadingWith("serverConsume", () => serverConsumeCount)
                .WritingWith<int>("clientConsume", (value) => clientConsumeCount += value)
                .ReadingWith("clientConsume", () => clientConsumeCount);
            _serverConsumer.UntilConsume = accessSafely;
            _clientConsumer.UntilConsume = accessSafely;
            
            // simulate network latency for parts of single request
            Request(requestPart1);
            Thread.Sleep(100);
            Request(requestPart2);
            Thread.Sleep(200);
            Request(requestPart3);
            while (_serverConsumer.UntilConsume.ReadFrom<int>("serverConsume") < 1)
            {
                ;
            }
            _serverConsumer.UntilConsume.ReadFromExpecting("serverConsume", 1);
            
            while (_clientConsumer.UntilConsume.ReadFrom<int>("clientConsume") < 1)
            {
                _client.ProbeChannel();
            }
            
            _clientConsumer.UntilConsume.ReadFromExpecting("clientConsume", 1);
            
            Assert.Equal(1, _serverConsumer.UntilConsume.ReadFrom<int>("serverConsume"));
            Assert.Equal(1, serverConsumeCount);
            Assert.Equal(serverConsumeCount, _serverConsumer.Requests.Count);
            
            Assert.Equal(1, _clientConsumer.UntilConsume.ReadFrom<int>("clientConsume"));
            Assert.Equal(1, clientConsumeCount);
            Assert.Equal(clientConsumeCount, _clientConsumer.Responses.Count);
            
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
            
            var serverConsumeCount = 0;
            var clientConsumeCount = 0;
            var accessSafely = AccessSafely.AfterCompleting(total)
                .WritingWith<int>("serverConsume", (value) => serverConsumeCount += value)
                .ReadingWith("serverConsume", () => serverConsumeCount)
                .WritingWith<int>("clientConsume", (value) => clientConsumeCount += value)
                .ReadingWith("clientConsume", () => clientConsumeCount);
            _serverConsumer.UntilConsume = accessSafely;
            _clientConsumer.UntilConsume = accessSafely;

            for (var idx = 0; idx < total; ++idx)
            {
                Request(request + idx);
            }

            while (_clientConsumer.UntilConsume.ReadFrom<int>("clientConsume") < total)
            {
                _client.ProbeChannel();
            }

            _serverConsumer.UntilConsume.ReadFromExpecting("serverConsume", total);
            _clientConsumer.UntilConsume.ReadFromExpecting("clientConsume", total);

            Assert.Equal(total, _serverConsumer.UntilConsume.ReadFrom<int>("serverConsume"));
            Assert.Equal(total, serverConsumeCount);
            Assert.Equal(total, _serverConsumer.Requests.Count);

            Assert.Equal(total, _clientConsumer.UntilConsume.ReadFrom<int>("clientConsume"));
            Assert.Equal(total, clientConsumeCount);
            Assert.Equal(total, _clientConsumer.Responses.Count);
    
            for (int idx = 0; idx < total; ++idx)
            {
                Assert.Equal(_clientConsumer.Responses[idx], _serverConsumer.Requests[idx]);
            }
        }
        
        [Fact]
        public void TestThatRequestResponsePoolLimitsNotExceeded()
        {
            _output.WriteLine("Starting TestThatRequestResponsePoolLimitsNotExceeded");
            var total = PoolSize * 2;
            var request = "Hello, Request-Response";
            
            _serverConsumer.CurrentExpectedRequestLength = request.Length + 3; // digits 000 - 999
            _clientConsumer.CurrentExpectedResponseLength = _serverConsumer.CurrentExpectedRequestLength;

            var serverConsumeCount = 0;
            var clientConsumeCount = 0;
            var accessSafely = AccessSafely.AfterCompleting(total)
                .WritingWith<int>("serverConsume", (value) => serverConsumeCount += value)
                .ReadingWith("serverConsume", () => serverConsumeCount)
                .WritingWith<int>("clientConsume", (value) => clientConsumeCount += value)
                .ReadingWith("clientConsume", () => clientConsumeCount);
            _serverConsumer.UntilConsume = accessSafely;
            _clientConsumer.UntilConsume = accessSafely;
    
            for (int idx = 0; idx < total; ++idx)
            {
                Request(request + idx.ToString("D3"));
            }
            
            Thread.Sleep(100);

            while (_clientConsumer.UntilConsume.ReadFrom<int>("clientConsume") < total)
            {
                _client.ProbeChannel();
            }

            _serverConsumer.UntilConsume.ReadFromExpecting("serverConsume", total);
            _clientConsumer.UntilConsume.ReadFromExpecting("clientConsume", total);
            
            Assert.Equal(total, _serverConsumer.UntilConsume.ReadFrom<int>("serverConsume"));
            Assert.Equal(total, serverConsumeCount);
            Assert.Equal(serverConsumeCount, _serverConsumer.Requests.Count);

            Assert.Equal(total, _clientConsumer.UntilConsume.ReadFrom<int>("clientConsume"));
            Assert.Equal(total, clientConsumeCount);
            Assert.Equal(clientConsumeCount, _clientConsumer.Responses.Count);

            for (int idx = 0; idx < total; ++idx) 
            {
                Assert.Equal(_clientConsumer.Responses[idx], _serverConsumer.Requests[idx]);
            }
        }

        public SocketRequestResponseChannelTest(ITestOutputHelper output)
        {
            _output = output;
            var converter = new Converter(output);
            Console.SetOut(converter);

            _world = World.StartWithDefaults("test-request-response-channel");
            
            _buffer = new MemoryStream(1024);
            var logger = ConsoleLogger.TestInstance();
            var provider = new TestRequestChannelConsumerProvider();
            _serverConsumer = (TestRequestChannelConsumer)provider.Consumer;

            _server = ServerRequestResponseChannelFactory.Start(
                _world.Stage,
                provider,
                _testPort,
                "test-server",
                1,
                PoolSize,
                10240,
                10L);
            
            _clientConsumer = new TestResponseChannelConsumer();
            
            _client = new BasicClientRequestResponseChannel(Address.From(Host.Of("localhost"), _testPort, AddressType.None),
                _clientConsumer, PoolSize, 10240, logger);

            ++_testPort;
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