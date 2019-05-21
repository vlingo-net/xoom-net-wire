// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    using Common;
    
    public sealed class SocketChannelSelectionProcessorActor : Actor,
                                                        ISocketChannelSelectionProcessor,
                                                        IResponseSenderChannel<Socket>,
                                                        IScheduled<object>
    {
        private int _bufferId;
        private readonly ICancellable _cancellable;
        private int _contextId;
        private readonly int _messageBufferSize;
        private readonly string _name;
        private readonly IRequestChannelConsumerProvider _provider;
        private readonly IResponseSenderChannel<Socket> _responder;
        private Context _context;
        
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
            _responder = SelfAs<IResponseSenderChannel<Socket>>();
            _cancellable = Stage.Scheduler.Schedule(SelfAs<IScheduled<object>>(),
                null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(probeInterval));
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
        
        public async void Process(Socket channel)
        {
            try
            {
                if (channel.Poll(10000, SelectMode.SelectRead))
                {
                    var clientChannel = await channel.AcceptAsync();
                    _context = new Context(this, clientChannel);
                }
            }
            catch (ObjectDisposedException e)
            {
                Logger.Log($"The underlying channel for {_name} is closed. This is certainly because Actor was stopped.", e);
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

        public async void IntervalSignal(IScheduled<object> scheduled, object data)
        {
            try
            {
                await ProbeChannel();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    Logger.Log($"Failed to ProbeChannel for {_name} because: {e.Message}", e);
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

            try
            {
                _context.Close();
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to close client context '{_context.Id}' socket for {_name} while stopping because: {e.Message}", e);
            }
        }
        
        //=========================================
        // internal implementation
        //=========================================

        private void Close(Socket channel, Context context)
        {
            try
            {
                channel.Close();
            }
            catch
            {
                // already closed; ignore
            }
            
            try
            {
                context.Close();
            }
            catch
            {
                // already closed; ignore
            }
        }

        private async Task ProbeChannel()
        {
            if (IsStopped)
            {
                return;
            }

            try
            {
                if (_context != null && _context.Channel.IsSocketConnected())
                {
                    if (_context.Channel.Available > 0)
                    {
                        await Read(_context);
                    }
                    else
                    {
                        await Write(_context);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed client channel processing for {_name} because: {e.Message}", e);
            }
        }
        
        private async Task Read(Context readable)
        {
            var channel = readable.Channel;
            if (!channel.IsSocketConnected())
            {
                readable.Close();
                channel.Close();
                return;
            }

            var buffer = readable.RequestBuffer.Clear();
            var readBuffer = buffer.ToArray();
            var totalBytesRead = 0;
            int bytesRead;

            try
            {
                do
                {
                    bytesRead = await channel.ReceiveAsync(readBuffer, SocketFlags.None);
                    buffer.Put(readBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                } while (channel.Available > 0);
            }
            catch
            {
                // likely a forcible close by the client,
                // so force close and cleanup
                bytesRead = 0;
            }

            if (bytesRead == 0)
            {
                Close(readable.Channel, readable);
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

        private async Task Write(Context writable)
        {
            var channel = writable.Channel;
            if (!channel.IsSocketConnected())
            {
                writable.Close();
                channel.Close();
                return;
            }
            
            if (writable.HasNextWritable)
            {
                await WriteWithCachedData(writable, channel);
            }
        }

        private async Task WriteWithCachedData(Context context, Socket channel)
        {
            for (var buffer = context.NextWritable(); buffer != null; buffer = context.NextWritable())
            {
                await WriteWithCachedData(context, channel, buffer);
            }
        }

        private async Task WriteWithCachedData(Context context, Socket clientChannel, IConsumerByteBuffer buffer)
        {
            try
            {
                var responseBuffer = buffer.ToArray();
                await clientChannel.SendAsync(new ArraySegment<byte>(responseBuffer), SocketFlags.None);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to write buffer for {_name} with channel {clientChannel.RemoteEndPoint} because: {e.Message}", e);
            }
            finally
            {
                buffer.Release();
            }
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

            public bool HasNextWritable => _writables.Count > 0;

            public IConsumerByteBuffer NextWritable()
            {
                if (HasNextWritable)
                {
                    return _writables.Dequeue();
                }

                return null;
            }

            public void QueueWritable(IConsumerByteBuffer buffer) => _writables.Enqueue(buffer);

            public IConsumerByteBuffer RequestBuffer => _buffer;

            public Socket Channel => _clientChannel;
        }
    }
}