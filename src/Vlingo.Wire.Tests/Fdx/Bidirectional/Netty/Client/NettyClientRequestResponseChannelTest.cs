// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Vlingo.Actors;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Common;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Bidirectional.Netty.Client;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Fdx.Bidirectional.Netty.Client
{
    public class NettyClientRequestResponseChannelTest
    {
        private static readonly AtomicInteger TestPort = new AtomicInteger(37870);

        [Fact(Skip = "Debugging CI")]
        public void TestServerNotAvailableBecauseOfConnectionTimeout()
        {
            var address = Address.From(Host.Of("localhost"), 8980, AddressType.Main);
            var clientChannel = new NettyClientRequestResponseChannel(address, new ThrowingResponseChannelConsumer(), 1,
                1, TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), ConsoleLogger.TestInstance());

            var message = Assert.Throws<Exception>(() =>
                clientChannel.RequestWith(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).Message;
            Assert.Equal($"Connection timeout 1ms expired before the connection could be established.", message);
        }

        [Fact(Skip = "Debugging CI")]
        public void TestServerNotAvailable()
        {
            var address = Address.From(Host.Of("localhost"), 8981, AddressType.Main);
            var clientChannel = new NettyClientRequestResponseChannel(address, new ThrowingResponseChannelConsumer(), 1,
                1, TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), ConsoleLogger.TestInstance());

            Assert.Throws<ConnectException>(() =>
                clientChannel.RequestWith(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())));
        }

        [Fact]
        public async Task TestServerRequestReply()
        {
            var nrExpectedMessages = 200;
            var requestMsgSize = 36; //The length of UUID
            var replyMsSize = 36 + 6; //The length of request + length of " reply"

            var connectionsCount = new CountdownEvent(1);
            var serverReceivedMessagesCount = new CountdownEvent(nrExpectedMessages);

            var serverReceivedMessage = new List<string>();
            var serverSentMessages = new List<string>();

            var clientSentMessages = new List<string>();

            IChannel server = null;
            var parentGroup = new MultithreadEventLoopGroup(1);
            var childGroup = new MultithreadEventLoopGroup();

            var logger = ConsoleLogger.TestInstance();
            try
            {
                var testPort = TestPort.IncrementAndGet();

                server = await BootstrapServer(requestMsgSize, connectionsCount, serverReceivedMessagesCount,
                    serverReceivedMessage, serverSentMessages, parentGroup, childGroup, testPort, logger);

                var clientConsumer = new TestResponseChannelConsumer();
                clientConsumer.CurrentExpectedResponseLength = replyMsSize;
                clientConsumer.CurrentState = new TestResponseChannelConsumer.State(nrExpectedMessages);

                var address = Address.From(Host.Of("localhost"), testPort, AddressType.Main);

                
                var clientChannel = new NettyClientRequestResponseChannel(address, clientConsumer, 10, replyMsSize,
                    TimeSpan.FromMilliseconds(1000), logger);

                for (var i = 0; i < nrExpectedMessages; i++)
                {
                    var request = Guid.NewGuid().ToString();
                    clientSentMessages.Add(request);
                    clientChannel.RequestWith(Encoding.UTF8.GetBytes(request));
                }

                connectionsCount.Wait();
                serverReceivedMessagesCount.Wait();

                Assert.Equal(0, clientConsumer.CurrentState.Access.ReadFrom<int>("remaining"));

                clientSentMessages.ForEach(clientRequest => Assert.Contains(clientRequest, serverReceivedMessage));

                serverSentMessages.ForEach(serverReply => Assert.True(clientConsumer.Responses.Contains(serverReply)));
            }
            finally
            {
                if (server != null)
                {
                    await server.CloseAsync();
                }

                parentGroup.ShutdownGracefullyAsync().Wait();
                childGroup.ShutdownGracefullyAsync().Wait();
            }
        }

        public NettyClientRequestResponseChannelTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
        }

        private Task<IChannel> BootstrapServer(
            int requestMsgSize,
            CountdownEvent connectionCount,
            CountdownEvent serverReceivedMessagesCount,
            List<string> serverReceivedMessage,
            List<string> serverSentMessages,
            IEventLoopGroup parentGroup,
            IEventLoopGroup childGroup,
            int testPort,
            ILogger logger)
        {
            var b = new ServerBootstrap();

            return b.Group(parentGroup, childGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new LoggingHandler("SRV-LSTN", LogLevel.TRACE))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(
                    ch =>
                    {
                        ch.Pipeline.AddLast(new LoggingHandler("SRV-CONN", LogLevel.TRACE));
                        ch.Pipeline.AddLast(new ChannelHandlerAdapterMock(
                            requestMsgSize, connectionCount,
                            serverReceivedMessagesCount, serverReceivedMessage, serverSentMessages, logger));
                    }))
                .BindAsync(new IPEndPoint(IPAddress.Any, testPort));
        }

        private class ChannelHandlerAdapterMock : ChannelHandlerAdapter
        {
            private byte[] _partialRequestBytes;
            private readonly int _requestMsgSize;
            private readonly CountdownEvent _connectionCount;
            private readonly CountdownEvent _serverReceivedMessagesCount;
            private readonly List<string> _serverReceivedMessage;
            private readonly List<string> _serverSentMessages;
            private readonly ILogger _logger;

            public ChannelHandlerAdapterMock(
                int requestMsgSize,
                CountdownEvent connectionCount,
                CountdownEvent serverReceivedMessagesCount,
                List<string> serverReceivedMessage,
                List<string> serverSentMessages,
                ILogger logger)
            {
                _requestMsgSize = requestMsgSize;
                _connectionCount = connectionCount;
                _serverReceivedMessagesCount = serverReceivedMessagesCount;
                _serverReceivedMessage = serverReceivedMessage;
                _serverSentMessages = serverSentMessages;
                _logger = logger;
            }

            public override void ChannelActive(IChannelHandlerContext context)
            {
                base.ChannelActive(context);
                _connectionCount.Signal();
                _logger.Info($"Connection made for: {context.Name}");
            }

            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                var byteBuf = (IByteBuffer) message;

                while (byteBuf.ReadableBytes >= _requestMsgSize)
                {
                    string request;
                    if (_partialRequestBytes != null && _partialRequestBytes.Length > 0)
                    {
                        var remainingBytes = _requestMsgSize - _partialRequestBytes.Length;
                        request =
                            $"{Encoding.UTF8.GetString(_partialRequestBytes)}{byteBuf.ReadCharSequence(remainingBytes, Encoding.Default).ToString()}";
                        _partialRequestBytes = null;
                    }
                    else
                    {
                        request = byteBuf.ReadCharSequence(_requestMsgSize, Encoding.Default).ToString();
                    }

                    _serverReceivedMessagesCount.Signal();
                    _serverReceivedMessage.Add(request);

                    var reply = $"{request} reply";

                    _serverSentMessages.Add(reply);

                    var bytes = Encoding.UTF8.GetBytes(reply);
                    var replyBuffer = context.Allocator.Buffer(bytes.Length);

                    replyBuffer.WriteBytes(bytes);

                    _logger.Info($"Write answer #{_serverReceivedMessage.Count}: {reply}");
                    context.WriteAndFlushAsync(replyBuffer);
                }

                if (byteBuf.ReadableBytes > 0)
                {
                    _partialRequestBytes = new byte[byteBuf.ReadableBytes];
                    byteBuf.ReadBytes(_partialRequestBytes);
                }
            }
        }

        private class ThrowingResponseChannelConsumer : IResponseChannelConsumer
        {
            public void Consume(IConsumerByteBuffer buffer) => throw new Exception("No replies are expected");
        }
    }
}