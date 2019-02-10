// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Vlingo.Actors;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    using Common;
    
    public class SocketChannelSelectionProcessorActor : Actor,
                                                        ISocketChannelSelectionProcessor,
                                                        IResponseSenderChannel,
                                                        IScheduled
    {
        private int _bufferId;
        private readonly ICancellable _cancellable;
        private int _contextId;
        private readonly int _messageBufferSize;
        private readonly string _name;
        private readonly IRequestChannelConsumerProvider _provider;
        private readonly IResponseSenderChannel _responder;

        public SocketChannelSelectionProcessorActor(
            IRequestChannelConsumerProvider provider,
            string name,
            int maxBufferPoolSize,
            int messageBufferSize,
            long probeInterval)
        {
            _provider = provider;
            _name = name;
            _messageBufferSize = messageBufferSize;
            _responder = SelfAs<IResponseSenderChannel>();
            _cancellable = Stage.Scheduler.Schedule(SelfAs<IScheduled>(), null, 100, probeInterval);
        }
        
        //=========================================
        // ResponseSenderChannel
        //=========================================
        
        public void Close()
        {
            if (IsStopped)
            {
                return;
            }
            
            SelfAs<IStoppable>().Stop();
        }

        public void Abandon<T>(RequestResponseContext<T> context)
        {
            throw new System.NotImplementedException();
        }

        public void RespondWith<T>(RequestResponseContext<T> context, IConsumerByteBuffer buffer)
        {
            throw new System.NotImplementedException();
        }
        
        public void Process(Socket channel)
        {
            throw new System.NotImplementedException();
        }

        public void IntervalSignal(IScheduled scheduled, object data)
        {
            throw new System.NotImplementedException();
        }
        
        //=========================================
        // internal implementation
        //=========================================

        private class Context : RequestResponseContext<Socket>
        {
            private readonly IConsumerByteBuffer _buffer;
            private readonly SocketChannelSelectionProcessorActor _parent;
            private readonly Socket _clientChannel;
            private object _closingData;
            private readonly IRequestChannelConsumer _consumer;
            private object _consumerData;
            private readonly string _id;
            private readonly Queue<IConsumerByteBuffer> _writables;

            public Context(SocketChannelSelectionProcessorActor parent, Socket clientChannel)
            {
                _parent = parent;
                _clientChannel = clientChannel;
                _consumer = parent._provider.RequestChannelConsumer();
                _buffer = BasicConsumerByteBuffer.Allocate(++_parent._bufferId, _parent._messageBufferSize);
                _id = $"{++_parent._contextId}";
                _writables = new Queue<IConsumerByteBuffer>();
            }

            public override T ConsumerData<T>() => (T) _consumerData;

            public override T ConsumerData<T>(T workingData)
            {
                _consumerData = workingData;
                return workingData;
            }

            public override bool HasConsumerData => _consumerData != null;
            
            public override string Id => _id;
            public override IResponseSenderChannel Sender => _parent._responder;

            public override void WhenClosing(object data) => _closingData = data;

            void Close()
            {
                if (!_clientChannel.IsSocketConnected())
                {
                    return;
                }

                try
                {
                    _consumer.CloseWith(this, _closingData);
                    _clientChannel.Close();
                }
                catch (Exception e)
                {
                    _parent.Logger.Log($"Failed to close client channel for {_parent._name} because: {e.Message}", e);
                }
            }

            IRequestChannelConsumer Consumer => _consumer;

            bool HasNextWritable => _writables.Peek() != null;

            IConsumerByteBuffer NextWritable() => _writables.Dequeue();

            void QueueWritable(IConsumerByteBuffer buffer) => _writables.Enqueue(buffer);

            IConsumerByteBuffer RequestBuffer => _buffer;
        }
    }
}