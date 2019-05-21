// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    using Common;
    
    public sealed class ServerRequestResponseChannelActor : Actor, IServerRequestResponseChannel, IScheduled<object>
    {
        private readonly ICancellable _cancellable;
        private readonly Socket _channel;
        private readonly string _name;
        private readonly ISocketChannelSelectionProcessor[] _processors;
        private int _processorPoolIndex;
        
        public ServerRequestResponseChannelActor(
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval)
        {
            _name = name;
            _processors = StartProcessors(provider, name, processorPoolSize, maxBufferPoolSize, maxMessageSize, probeInterval);

            try
            {
                Logger.Log($"{GetType().Name}: OPENING PORT: {port}");
                _channel = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _channel.Bind(new IPEndPoint(IPAddress.Any, port));
                _channel.Listen(120);
            }
            catch (Exception e)
            {
                Logger.Log($"Failure opening socket because: {e.Message}", e);
                throw;
            }

            _cancellable = Stage.Scheduler.Schedule(SelfAs<IScheduled<object>>(),
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
            long probeInterval)
        {
            return ServerRequestResponseChannelFactory.Start(stage, provider, port, name, processorPoolSize,
                maxBufferPoolSize, maxMessageSize, probeInterval);
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
            long probeInterval)
        {
            return ServerRequestResponseChannelFactory.Start(stage, address, mailboxName, provider, port, name, processorPoolSize,
                maxBufferPoolSize, maxMessageSize, probeInterval);
        }
        
        public void Close()
        {
            if (IsStopped)
            {
                return;
            }
            
            SelfAs<IStoppable>().Stop();
        }
        
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
                Logger.Log($"Failed to close channel for: '{_name}'", e);
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
                Logger.Log($"Failed to accept client channel for '{_name}' because: {e.Message}", e);
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
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval)
        {
            var processors = new ISocketChannelSelectionProcessor[processorPoolSize];

            for (int idx = 0; idx < processors.Length; ++idx)
            {
                processors[idx] = ChildActorFor<ISocketChannelSelectionProcessor>(
                    Definition.Has<SocketChannelSelectionProcessorActor>(
                        Definition.Parameters(provider, $"{name}-processor-{idx}", maxBufferPoolSize, maxMessageSize, probeInterval)));
            }

            return processors;
        }
    }
}