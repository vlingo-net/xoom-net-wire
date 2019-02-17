// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net.Sockets;
using Vlingo.Actors;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    using Common;
    
    public sealed class ServerRequestResponseChannelActor : Actor, IServerRequestResponseChannel, IScheduled
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

            }
            catch (Exception e)
            {
                Logger.Log($"Failure opening socket because: {e.Message}", e);
                throw;
            }
        }

        public IServerRequestResponseChannel Start(
            Stage stage,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval)
        {
            throw new System.NotImplementedException();
        }

        public IServerRequestResponseChannel Start(
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
            throw new System.NotImplementedException();
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void IntervalSignal(IScheduled scheduled, object data)
        {
            throw new System.NotImplementedException();
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