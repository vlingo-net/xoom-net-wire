// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    using Common;
    
    public class SocketChannelSelectionProcessorActor : Actor,
                                                        ISocketChannelSelectionProcessor,
                                                        IResponseSenderChannel<Socket>,
                                                        IScheduled
    {
        private int _bufferId;
        private readonly ICancellable _cancellable;
        private int _contextId;
        private readonly int _messageBufferSize;
        private readonly string _name;
        private readonly IRequestChannelConsumerProvider _provider;
        private readonly IResponseSenderChannel<Socket> _responder;
        private readonly ConcurrentDictionary<string, Context> _contexts;

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
            _contexts = new ConcurrentDictionary<string, Context>();
            _responder = SelfAs<IResponseSenderChannel<Socket>>();
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

        public void Abandon(RequestResponseContext<Socket> context) => ((Context)context).Close();

        public void RespondWith(RequestResponseContext<Socket> context, IConsumerByteBuffer buffer) =>
            ((Context) context).QueueWritable(buffer);
        
        //=========================================
        // SocketChannelSelectionProcessor
        //=========================================
        
        public async Task ProcessAsync(Socket channel)
        {
            try
            {
                if (channel.IsSocketConnected())
                {
                    var clientChannel = await channel.AcceptAsync();
                    if (clientChannel != null)
                    {
                        clientChannel.Blocking = false;
                        var context = new Context(this, clientChannel);
                        if (!_contexts.TryAdd(context.Id, context))
                        {
                            throw new InvalidOperationException($"Unable to add context for id '{context.Id}'");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var message = $"Failed to accept client socket for {_name} because: {e.Message}";
                Logger.Log(message, e);
                throw;
            }
        }
        
        //=========================================
        // Scheduled
        //=========================================

        public void IntervalSignal(IScheduled scheduled, object data)
        {
            try
            {
                // this is invoked in the context of another Thread so even if we can block here
                // TODO: should be a better way than blocking
                ProbeChannelAsync().Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    Logger.Log($"Failed to ProbeChannelAsync for {_name} because: {e.Message}", e);
                }
                
                throw ae.Flatten();
            }
        }

        //=========================================
        // Stoppable
        //=========================================

        public override void Stop()
        {
            _cancellable.Cancel();

            foreach (var key in _contexts.Keys)
            {
                try
                {
                    _contexts[key].Close();
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to close client context '{key}' socket for {_name} while stopping because: {e.Message}", e);
                }
            }
            
            _contexts.Clear();
        }
        
        //=========================================
        // internal implementation
        //=========================================

        private async Task ProbeChannelAsync()
        {
            if (IsStopped)
            {
                return;
            }

            try
            {
                var copy = _contexts.Values.ToArray();
                var checkRead = new List<Context>(copy);
                var checkWrite = new List<Context>(copy);
                Socket.Select(checkRead, checkWrite, null, 1000);

                foreach (var readable in checkRead)
                {
                    await ReadAsync(readable);
                }
                
                foreach (var writable in checkWrite)
                {
                    Write(writable);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed client channel processing for {_name} because: {e.Message}", e);
            }
        }
        
        private async Task ReadAsync(Context readable)
        {
            var channel = readable.Channel;
            if (!channel.IsSocketConnected())
            {
                _contexts.TryRemove(readable.Id, out var removed);
                removed.Close();
                channel.Close();
                return;
            }

            var buffer = readable.RequestBuffer.Clear();
            var readBuffer = buffer.Array();
            var totalBytesRead = 0;
            var bytesRead = 0;
            do
            {
                bytesRead = await channel.ReceiveAsync(new ArraySegment<byte>(readBuffer), SocketFlags.None).ConfigureAwait(false);
                totalBytesRead += bytesRead;
            } while (bytesRead > 0);

            if (bytesRead == 0)
            {
                _contexts.TryRemove(readable.Id, out var removed);
                removed.Close();
                channel.Close();
            }
            
            if (totalBytesRead > 0)
            {
                readable.Consumer.Consume(readable, buffer.Flip());
            } 
            else 
            {
                buffer.Release();
            }
        }

        private void Write(Context writable)
        {
            throw new NotImplementedException();
        }

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
            
            public override IResponseSenderChannel<Socket> Sender => _parent._responder;

            public override void WhenClosing(object data) => _closingData = data;

            public void Close()
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

            public IRequestChannelConsumer Consumer => _consumer;

            public bool HasNextWritable => _writables.Peek() != null;

            public IConsumerByteBuffer NextWritable() => _writables.Dequeue();

            public void QueueWritable(IConsumerByteBuffer buffer) => _writables.Enqueue(buffer);

            public IConsumerByteBuffer RequestBuffer => _buffer;

            public Socket Channel => _clientChannel;
        }
    }
}