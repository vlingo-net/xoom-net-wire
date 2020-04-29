// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Common.Pool;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty.Server
{
    public class NettyInboundHandler : ChannelHandlerAdapter, IResponseSenderChannel
    {
        private readonly ILogger _logger;
        private static readonly string WireContextName = "$WIRE_CONTEXT";

        private static readonly AttributeKey<NettyServerChannelContext> WireContext =
            AttributeKey<NettyServerChannelContext>.Exists(WireContextName)
                ? AttributeKey<NettyServerChannelContext>.ValueOf(WireContextName)
                : AttributeKey<NettyServerChannelContext>.NewInstance(WireContextName);

        private readonly IRequestChannelConsumer _consumer;
        private string? _contextInstanceId;
        private readonly ConsumerByteBufferPool _readBufferPool;

        private static readonly AtomicLong NextInstanceId = new AtomicLong(0);
        private readonly long _instanceId;

        public NettyInboundHandler(IRequestChannelConsumerProvider consumerProvider, int maxBufferPoolSize,
            int maxMessageSize, ILogger logger)
        {
            _logger = logger;
            _consumer = consumerProvider.RequestChannelConsumer();
            _readBufferPool =
                new ConsumerByteBufferPool(
                    ElasticResourcePool<IConsumerByteBuffer, string>.Config.Of(maxBufferPoolSize), maxMessageSize);
            _instanceId = NextInstanceId.IncrementAndGet();
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            _logger.Debug(
                $">>>>> NettyInboundHandler.ChannelActive(): {_instanceId} NAME: {ContextInstanceId(context)}");
            if (context.Channel.Active)
            {
                GetWireContext(context);
            }
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message == null || Equals(message, Unpooled.Empty) || message is EmptyByteBuffer)
            {
                return;
            }
            
            _logger.Debug($">>>>> NettyInboundHandler.ChannelRead(): {_instanceId} NAME: {ContextInstanceId(context)}");

            try
            {
                var channelContext = GetWireContext(context);

                var pooledBuffer = _readBufferPool.Acquire("NettyClientHandler#channelRead");

                try
                {
                    var byteBuf = (IByteBuffer) message;
                    byte[] bytes = new byte[byteBuf.ReadableBytes];
                    byteBuf.ReadBytes(bytes);

                    pooledBuffer.Put(bytes);

                    _consumer.Consume(channelContext, pooledBuffer.Flip());
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
                context.CloseAsync();
            }
            finally
            {
                ReferenceCountUtil.Release(message);
            }
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            _logger.Debug(
                $">>>>> NettyInboundHandler.ChannelUnregistered(): {_instanceId} NAME: {ContextInstanceId(context)}");
            base.ChannelUnregistered(context);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _logger.Error("Unexpected exception", exception);
            base.ExceptionCaught(context, exception);
        }

        public void Abandon(RequestResponseContext context)
        {
            var nettyChannelContext = ((NettyServerChannelContext) context).NettyChannelContext;
            _logger.Debug($">>>>> NettyInboundHandler.Abandon(): {_instanceId} NAME: {ContextInstanceId(nettyChannelContext)}");
            nettyChannelContext.CloseAsync();
        }

        public void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer) =>
            RespondWith(context, buffer, false);

        public void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer, bool closeFollowing)
        {
            var nettyServerChannelContext = (NettyServerChannelContext) context;
            var channelHandlerContext = nettyServerChannelContext.NettyChannelContext;

            var contextInstanceId = ContextInstanceId(channelHandlerContext);
            _logger.Debug(
                $">>>>> NettyInboundHandler.RespondWith(): {_instanceId} NAME: {contextInstanceId} : CLOSE? {closeFollowing}");

            var replyBuffer = channelHandlerContext.Allocator.Buffer((int) buffer.Limit());

            replyBuffer.WriteBytes(buffer.ToArray());

            channelHandlerContext
                .WriteAndFlushAsync(replyBuffer)
                .ContinueWith(written =>
                {
                    // written.IsCompletedSuccessfully in standard >= 2.1
                    if (written.IsCompleted && !(written.IsCanceled || written.IsFaulted))
                    {
                        _logger.Trace("Reply sent (DotNetty)");
                    }
                    else
                    {
                        _logger.Error("Failed to send reply", written.Exception);
                        
                        if (closeFollowing)
                        {
                            CloseConnection(channelHandlerContext, contextInstanceId);
                        }
                    }
                });
        }

        private void CloseConnection(IChannelHandlerContext channelHandlerContext, string contextInstanceId)
        {
            CloseAsync(channelHandlerContext)
                .ContinueWith(closed =>
                {
                    if (closed.IsCompleted && !(closed.IsCanceled || closed.IsFaulted))
                    {
                        _logger.Debug(
                            $">>>>> NettyInboundHandler.RespondWith(): {_instanceId} NAME: {contextInstanceId} : CLOSED");
                    }
                    else
                    {
                        _logger.Debug(
                            $">>>>> NettyInboundHandler.RespondWith(): {_instanceId} NAME: {contextInstanceId} : FAILED TO CLOSE");
                    }
                });
        }

        public void RespondWith(RequestResponseContext context, object response, bool closeFollowing)
        {
            var textResponse = response.ToString();

            var buffer = new BasicConsumerByteBuffer(0, textResponse.Length + 1024)
                    .Put(Encoding.UTF8.GetBytes(textResponse)).Flip();

            RespondWith(context, buffer, closeFollowing);
        }

        private NettyServerChannelContext GetWireContext(IChannelHandlerContext ctx)
        {
            var nettyChannel = ctx.Channel;
            if (!nettyChannel.HasAttribute(WireContext))
            {
                nettyChannel.GetAttribute(WireContext).Set(new NettyServerChannelContext(ctx, this));
            }

            return nettyChannel.GetAttribute(WireContext).Get();
        }

        private string ContextInstanceId(IChannelHandlerContext context)
        {
            if (_contextInstanceId == null)
            {
                _contextInstanceId = $"{context.Name}:{_instanceId}";
            }

            return _contextInstanceId;
        }
    }
}