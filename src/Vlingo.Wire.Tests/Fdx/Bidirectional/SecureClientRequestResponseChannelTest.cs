// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using Vlingo.Wire.Fdx.Bidirectional;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;
using Vlingo.Xoom.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional
{
    public class SecureClientRequestResponseChannelTest : IDisposable
    {
        private static int _poolSize = 100;
        
        private IClientRequestResponseChannel _client;
        private readonly World _world;
        private readonly TestSecureResponseChannelConsumer _clientConsumer;

        [Fact]
        public void TestThatSecureClientRequestResponse()
        {
            var address = Address.From(Host.Of("www.google.com"), 443, AddressType.None);
            _client = new SecureClientRequestResponseChannel(address, _clientConsumer, _poolSize, 65536, _world.DefaultLogger);

            _clientConsumer.CurrentExpectedResponseLength = 500;
            var access = _clientConsumer.AfterCompleting(1);

            var get = "GET / HTTP/1.1\nHost: www.google.com\nConnection: close\n\n";
            var buffer = BasicConsumerByteBuffer.Allocate(1, 1000);
            buffer.Put(Encoding.UTF8.GetBytes(get));
            buffer.Flip();
            _client.RequestWith(buffer.ToArray());

            for (int count = 0; count < 100; ++count)
            {
                if (access.TotalWrites > 0) break;
                _client.ProbeChannel();
                Thread.Sleep(100);
            }

            Assert.True(_clientConsumer.GetConsumeCount() > 0);
            Assert.Contains("google.com", _clientConsumer.GetResponses().First());
        }
        
        public SecureClientRequestResponseChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);

            _world = World.StartWithDefaults("test-request-response-channel");

            _clientConsumer = new TestSecureResponseChannelConsumer();
        }

        public void Dispose()
        {
            _client.Close();
            
            _world.Terminate();
        }
    }
}