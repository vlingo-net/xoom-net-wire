// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Sockets;
using Vlingo.Actors;
using Vlingo.Common.Pool;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    using Common;
    
    public sealed class ServerRequestResponseChannelActor : Actor, IServerRequestResponseChannel, IScheduled<object>
    {
        private readonly ICancellable _cancellable;
        private readonly Socket _channel;
        private readonly string _name;
        private readonly ISocketChannelSelectionProcessor[] _processors;
        private readonly int _port;
        private int _processorPoolIndex;

        public ServerRequestResponseChannelActor(
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout)
        {
            _name = name;
            _port = port;
            var requestBufferPool = new ConsumerByteBufferPool(ElasticResourcePool<IConsumerByteBuffer, Nothing>.Config.Of(maxBufferPoolSize), maxMessageSize);
            _processors = StartProcessors(provider, name, processorPoolSize, requestBufferPool, probeInterval, probeTimeout);

            try
            {
                Logger.Info($"{GetType().Name}: OPENING PORT: {port}");
                _channel = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _channel.Bind(new IPEndPoint(IPAddress.Any, port));
                _channel.Listen(120);
            }
            catch (Exception e)
            {
                Logger.Error($"Failure opening socket because: {e.Message}", e);
                throw;
            }

            _cancellable = Stage.Scheduler.Schedule(SelfAs<IScheduled<object?>>(),
                null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(probeInterval));
        }
        
        //=========================================
        // ServerRequestResponseChannel
        //=========================================
        
        IServerRequestResponseChannel IServerRequestResponseChannel.Start(
            Stage stage,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout)
        {
            return ServerRequestResponseChannelFactory.Start(stage, provider, port, name, processorPoolSize,
                maxBufferPoolSize, maxMessageSize, probeInterval, probeTimeout);
        }

        IServerRequestResponseChannel IServerRequestResponseChannel.Start(
            Stage stage,
            IAddress address,
            string mailboxName,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout)
        {
            return ServerRequestResponseChannelFactory.Start(stage, address, mailboxName, provider, port, name, processorPoolSize,
                maxBufferPoolSize, maxMessageSize, probeInterval, probeTimeout);
        }
        
        public void Close()
        {
            if (IsStopped)
            {
                return;
            }
            
            SelfAs<IStoppable>().Stop();
        }

        public ICompletes<int> Port() => Completes().With(_port);

        //=========================================
        // Scheduled
        //=========================================
        
        public void IntervalSignal(IScheduled<object> scheduled, object data)
        {
            ProbeChannel();
        }
        
        //=========================================
        // Stoppable
        //=========================================

        public override void Stop()
        {
            _cancellable.Cancel();

            foreach (var socketChannelSelectionProcessor in _processors)
            {
                socketChannelSelectionProcessor.Close();
            }

            try
            {
                _channel.Close();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to close channel for: '{_name}'", e);
            }
            
            base.Stop();
        }

        //=========================================
        // internal implementation
        //=========================================
        private void ProbeChannel()
        {
            if (IsStopped)
            {
                return;
            }

            try
            {
                Accept(_channel);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to accept client channel for '{_name}' because: {e.Message}", e);
            }
        }

        private void Accept(Socket channel)
        {
            PooledProcessor().Process(channel);
        }

        private ISocketChannelSelectionProcessor PooledProcessor()
        {
            if (_processorPoolIndex >= _processors.Length)
            {
                _processorPoolIndex = 0;
            }

            return _processors[_processorPoolIndex++];
        }

        private ISocketChannelSelectionProcessor[] StartProcessors(
            IRequestChannelConsumerProvider provider,
            string name,
            int processorPoolSize,
            IResourcePool<IConsumerByteBuffer, Nothing> requestBufferPool,
            long probeInterval,
            long probeTimeout)
        {
            var processors = new ISocketChannelSelectionProcessor[processorPoolSize];

            for (int idx = 0; idx < processors.Length; ++idx)
            {
                processors[idx] = ChildActorFor<ISocketChannelSelectionProcessor>(
                    Definition.Has<SocketChannelSelectionProcessorActor>(
                        Definition.Parameters(provider, $"{name}-processor-{idx}", requestBufferPool, probeInterval, probeTimeout)));
            }

            return processors;
        }
    }
}