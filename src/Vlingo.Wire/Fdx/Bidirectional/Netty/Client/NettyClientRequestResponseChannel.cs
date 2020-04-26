// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty.Client
{
    /// <summary>
    /// Implementation of <see cref="IClientRequestResponseChannel"/> based on <a href="https://netty.io/wiki/user-guide-for-4.x.html">Netty</a>
    /// <para>
    /// <b>Important</b>. Due to streaming nature of TCP, there is no guarantee that what is read is exactly what server wrote.
    /// <see cref="IResponseChannelConsumer"/> might receive a <see cref="IConsumerByteBuffer"/> that contain a partial or event multiple server replies.
    /// Because of that, and the fact that the channel has no knowledge of the nature of messages being exchanged, <b>the <see cref="IResponseChannelConsumer"/> is responsible for extracting the message</b>.
    /// See Netty docs <a href="https://netty.io/wiki/user-guide-for-4.x.html#dealing-with-a-stream-based-transport">Dealing with a Stream-based Transport</a> for more information.
    /// </para>
    /// <para>
    /// This channel however guarantees that the <see cref="IConsumerByteBuffer"/> will never hold more bytes than what is configure by <c>maxMessageSize</c>.
    /// A received <see cref="IByteBuffer"/> with size greater than <c>maxMessageSize</c>, will be split into multiple <see cref="IByteBuffer"/>.
    /// </para>
    /// </summary>
    public class NettyClientRequestResponseChannel : IClientRequestResponseChannel
    {
        private readonly Address _address;
        private readonly IResponseChannelConsumer _consumer;
        private readonly int _maxBufferPoolSize;
        private readonly int _maxMessageSize;
        private TimeSpan _connectionTimeout;
        private Bootstrap? _bootstrap;
        private IChannel? _channel;
        private IEventLoopGroup? _workerGroup;
        private readonly TimeSpan _gracefulShutdownQuietPeriod;
        private readonly TimeSpan _gracefulShutdownTimeout;
        private readonly ILogger _logger;
        private readonly ManualResetEvent _connectDone;

        /// <summary>
        /// Build a instance of client channel with support of graceful shutdown.
        /// </summary>
        /// <param name="address">The address to connect to to</param>
        /// <param name="consumer"><see cref="IResponseChannelConsumer"/> for consuming the response buffers</param>
        /// <param name="maxBufferPoolSize"><see cref="IConsumerByteBuffer"/> size</param>
        /// <param name="maxMessageSize">Max message size</param>
        /// <param name="connectionTimeout">Connection timeout</param>
        /// <param name="gracefulShutdownQuietPeriod">Graceful shutdown ensures that no tasks are submitted for <i>'the quiet period'</i> before it shuts itself down.</param>
        /// <param name="gracefulShutdownTimeout">The maximum amount of time to wait until the Netty resources are shut down</param>
        /// <param name="logger">The current <see cref="ILogger"/></param>
        public NettyClientRequestResponseChannel(
            Address address,
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            TimeSpan connectionTimeout,
            TimeSpan gracefulShutdownQuietPeriod,
            TimeSpan gracefulShutdownTimeout,
            ILogger logger)
        {
            _address = address;
            _consumer = consumer;
            _maxBufferPoolSize = maxBufferPoolSize;
            _maxMessageSize = maxMessageSize;
            _connectionTimeout = connectionTimeout;
            _gracefulShutdownQuietPeriod = gracefulShutdownQuietPeriod;
            _gracefulShutdownTimeout = gracefulShutdownTimeout;
            _logger = logger;
            _connectDone = new ManualResetEvent(false);
        }

        public NettyClientRequestResponseChannel(
            Address address,
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            ILogger logger) : this(address, consumer, maxBufferPoolSize, maxMessageSize,
            TimeSpan.FromMilliseconds(1000), TimeSpan.Zero, TimeSpan.Zero, logger)
        {
        }

        public void Close()
        {
            try
            {
                // _channelTask.IsCompletedSuccessfully in standard >= 2.1
                if (_channel != null && _channel.Active)
                {
                    _channel.CloseAsync();
                }

                if (_workerGroup != null && !_workerGroup.IsShutdown)
                {
                    _workerGroup.ShutdownGracefullyAsync(_gracefulShutdownQuietPeriod, _gracefulShutdownTimeout);
                }

                _logger.Info($"Netty client actor for {_address} closed");
            }
            catch (Exception e)
            {
                _logger.Error($"Netty client actor for {_address} was not closed properly", e);
            }
            
            _connectDone.Reset();
        }

        public void RequestWith(byte[] buffer)
        {
            PrepareChannel();
            if (_channel != null)
            {
                var requestByteBuff = _channel.Allocator.Buffer(buffer.Length);

                requestByteBuff.WriteBytes(buffer);

                _channel.WriteAndFlushAsync(requestByteBuff) //IByteBuffer instance will be released by Netty
                    .ContinueWith(written =>
                    {
                        // written.IsCompletedSuccessfully in standard >= 2.1
                        if (written.IsCompleted && !(written.IsCanceled || written.IsFaulted))
                        {
                            _logger.Trace("Request sent (DotNetty)");
                        }
                        else
                        {
                            _logger.Error("Failed to send request", written.Exception);
                            // Close the channel in case of an error.
                            // Next request, or ProbeChannel() method invocation, will re-establish the connection
                            Close();
                        }
                    });   
            }
        }

        public void ProbeChannel() => PrepareChannel();

        private void PrepareChannel()
        {
            if (_workerGroup == null || _workerGroup.IsShutdown)
            {
                _workerGroup = new MultithreadEventLoopGroup();
            }

            if (_bootstrap == null)
            {
                _bootstrap = new Bootstrap();
                _bootstrap    
                    .Group(_workerGroup)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.SoKeepalive, true)
                    .Handler(new ActionChannelInitializer<TcpSocketChannel>(
                        ch => ch.Pipeline.AddLast(
                            //If DotNetty log level is configured as TRACE, will output the inbound/outbound data
                            new LoggingHandler(LogLevel.TRACE),
                            new MaxMessageSizeSplitter(_maxMessageSize),
                            new NettyChannelResponseHandler(_consumer, _maxBufferPoolSize, _maxMessageSize, _logger)
                        )))
                    .BeginConnect(_address.HostName, _address.Port, ConnectCallback, _bootstrap);
                if (!_connectDone.WaitOne(_connectionTimeout))
                {
                    throw new Exception($"Connection timeout {_connectionTimeout.TotalMilliseconds}ms expired before the connection could be established.");
                }
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var client = (Bootstrap) ar.AsyncState;
                _channel = client.EndConnect(ar);

                _connectDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to create client channel (DotNetty): {e.Message}", e);
                throw;
            }
        }

        private class MaxMessageSizeSplitter : ByteToMessageDecoder
        {
            private readonly int _maxMessageSize;

            public MaxMessageSizeSplitter(int maxMessageSize) => _maxMessageSize = maxMessageSize;

            protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
            {
                if (input.ReadableBytes < _maxMessageSize)
                {
                    output.Add(input.ReadBytes(input.ReadableBytes));
                }
                else
                {
                    while (input.ReadableBytes > 0)
                    {
                        if (input.ReadableBytes < _maxMessageSize)
                        {
                            output.Add(input.ReadBytes(input.ReadableBytes));
                        }
                        else
                        {
                            output.Add(input.ReadBytes(_maxMessageSize));
                        }
                    }
                }
            }
        }
    }
}