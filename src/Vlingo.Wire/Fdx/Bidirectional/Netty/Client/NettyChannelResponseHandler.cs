// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Vlingo.Actors;
using Vlingo.Common.Pool;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty.Client
{
    public class NettyChannelResponseHandler : ChannelHandlerAdapter
    {
        private IResponseChannelConsumer _consumer;
        private readonly ILogger _logger;
        private ConsumerByteBufferPool _readBufferPool;
        
        internal NettyChannelResponseHandler(IResponseChannelConsumer consumer, int maxBufferPoolSize, int maxMessageSize, ILogger logger)
        {
            _consumer = consumer;
            _logger = logger;
            _readBufferPool = new ConsumerByteBufferPool(ElasticResourcePool<IConsumerByteBuffer, string>.Config.Of(maxBufferPoolSize), maxMessageSize);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message == null || Equals(message, Unpooled.Empty) || message is EmptyByteBuffer)
            {
                return;
            }
            _logger.Trace("Response received");
            try
            {
                var pooledBuffer = _readBufferPool.Acquire("NettyClientChannel#channelRead");
                try
                {
                    var byteBuf = (IByteBuffer) message;
                    byte[] bytes = new byte[byteBuf.ReadableBytes];
                    byteBuf.ReadBytes(bytes);
                    pooledBuffer.Put(bytes);

                    _consumer.Consume(pooledBuffer.Flip());
                }
                catch (Exception)
                {
                    pooledBuffer.Release();
                    throw;
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error reading the incoming data.", e);
            }
            finally
            {
                ReferenceCountUtil.Release(message);
            }
        }
    }
}