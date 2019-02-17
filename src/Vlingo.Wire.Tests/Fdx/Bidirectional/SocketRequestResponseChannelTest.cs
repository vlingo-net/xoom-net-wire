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
using Vlingo.Wire.Fdx.Bidirectional;

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
    }
}