// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty.Server
{
    public class NettyServerChannelActor : Actor, IServerRequestResponseChannel
    {
        private readonly int _port;
        private readonly string _name;
        private readonly IEventLoopGroup _bossGroup;
        private readonly IEventLoopGroup _workerGroup;
        private IChannel? _channel;
        private readonly TimeSpan _gracefulShutdownQuietPeriod;
        private readonly TimeSpan _gracefulShutdownTimeout;
        private readonly ManualResetEvent _bindDone;

        public NettyServerChannelActor(
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long gracefulShutdownQuietPeriod,
            long gracefulShutdownTimeout)
        {
            _port = port;
            _name = name;
            _gracefulShutdownQuietPeriod = TimeSpan.FromMilliseconds(gracefulShutdownQuietPeriod);
            _gracefulShutdownTimeout = TimeSpan.FromMilliseconds(gracefulShutdownTimeout);
            _bossGroup = new MultithreadEventLoopGroup(processorPoolSize);
            _workerGroup = new MultithreadEventLoopGroup();
            _bindDone = new ManualResetEvent(false);

            try
            {
                var b = new ServerBootstrap();

                b.Group(_bossGroup, _workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .ChildHandler(
                        new ActionChannelInitializer<ISocketChannel>(channel =>
                            channel.Pipeline.AddLast(new NettyInboundHandler(provider, maxBufferPoolSize,
                                maxMessageSize, Logger))))
                    .BeginBind(port, BindCallback, b);
                _bindDone.WaitOne();

                Logger.Info("Netty server {} actor started", _name);
            }
            catch (Exception e)
            {
                Logger.Error("Netty server {} actor failed to initialize", _name, e);
                throw;
            }
        }

        private void BindCallback(IAsyncResult ar)
        {
            try
            {
                var client = (ServerBootstrap) ar.AsyncState;
                _channel = client.EndBind(ar);

                _bindDone.Set();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to bind server channel (DotNetty) on port {_port}: {e.Message}", e);
            }
        }

        public override void Stop()
        {
            Logger.Debug("Netty server actor {} will stop", _name);

            if (_channel != null && _channel.Active)
            {
                _channel.CloseAsync();
            }

            if (!_bossGroup.IsShutdown)
            {
                _bossGroup.ShutdownGracefullyAsync(_gracefulShutdownQuietPeriod, _gracefulShutdownTimeout);
            }
            
            if (!_workerGroup.IsShutdown)
            {
                _workerGroup.ShutdownGracefullyAsync(_gracefulShutdownQuietPeriod, _gracefulShutdownTimeout);
            }
            
            Logger.Info("Netty server actor {} closed", _name);
            
            base.Stop();
            
            _bindDone.Reset();
        }

        public IServerRequestResponseChannel Start(Stage stage, IRequestChannelConsumerProvider provider, int port,
            string name, int processorPoolSize, int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout) =>
            ServerRequestResponseChannelFactory.StartNetty(stage, provider, port, name, processorPoolSize,
                maxBufferPoolSize, maxMessageSize, probeInterval, probeTimeout);

        public IServerRequestResponseChannel Start(Stage stage, IAddress address, string mailboxName,
            IRequestChannelConsumerProvider provider, int port, string name, int processorPoolSize,
            int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout) =>
            ServerRequestResponseChannelFactory.StartNetty(stage, address, mailboxName, provider, port, name,
                processorPoolSize,
                maxBufferPoolSize, maxMessageSize, probeInterval, probeTimeout);

        public void Close()
        {
            if (IsStopped)
            {
                return;
            }
            
            SelfAs<IStoppable>().Stop();
        }

        public ICompletes<int> Port() => Common.Completes.WithSuccess(_port);
    }
}