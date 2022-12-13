// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Common;
using Vlingo.Xoom.Common.Pool;
using Vlingo.Xoom.Wire.Message;

namespace Vlingo.Xoom.Wire.Channel;

public sealed class SocketChannelSelectionProcessorActor : Actor,
    ISocketChannelSelectionProcessor,
    IResponseSenderChannel,
    IScheduled<object>
{
    private readonly ICancellable _cancellable;
    private int _contextId;
    private readonly string _name;
    private readonly long _probeTimeout;
    private readonly IRequestChannelConsumerProvider _provider;
    private readonly IResponseSenderChannel _responder;
    private readonly List<Context> _contexts;
    private bool _canStartProbing;
        
    private readonly IResourcePool<IConsumerByteBuffer, string> _requestBufferPool;

    public SocketChannelSelectionProcessorActor(
        IRequestChannelConsumerProvider provider,
        string name,
        IResourcePool<IConsumerByteBuffer, string> requestBufferPool,
        long probeInterval,
        long probeTimeout)
    {
        _provider = provider;
        _name = name;
        _requestBufferPool = requestBufferPool;
        _probeTimeout = probeTimeout;
        _contexts = new List<Context>();
        _responder = SelfAs<IResponseSenderChannel>();
        _cancellable = Stage.Scheduler.Schedule(SelfAs<IScheduled<object?>>(),
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

    public void Abandon(RequestResponseContext context) => ((Context)context).Close();

    public void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer) =>
        RespondWith(context, buffer, false);

    public void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer, bool closeFollowing)
    {
        ((Context) context).QueueWritable(buffer);
        ((Context) context).RequireExplicitClose(!closeFollowing);
    }

    public void RespondWith(RequestResponseContext context, object response, bool closeFollowing)
    {
        var textResponse = response.ToString();

        if (!string.IsNullOrEmpty(textResponse))
        {
            var buffer =
                new BasicConsumerByteBuffer(0, textResponse.Length + 1024)
                    .Put(Converters.TextToBytes(textResponse)).Flip();

            RespondWith(context, buffer, closeFollowing);
        }
    }

    //=========================================
    // SocketChannelSelectionProcessor
    //=========================================
        
    public void Process(Socket channel)
    {
        try
        {
            channel.BeginAccept(AcceptCallback, channel);
        }
        catch (ObjectDisposedException e)
        {
            Logger.Error($"The underlying channel for {_name} is closed. This is certainly because Actor was stopped.", e);
        }
        catch (Exception e)
        {
            var message = $"Failed to accept client socket for {_name} because: {e.Message}";
            Logger.Error(message, e);
            throw;
        }
    }

    //=========================================
    // Scheduled
    //=========================================

    public void IntervalSignal(IScheduled<object> scheduled, object data)
    {
        try
        {
            ProbeChannel();
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to ProbeChannel for {_name} because: {e.Message}", e);
        }
    }

    //=========================================
    // Stoppable
    //=========================================

    public override void Stop()
    {
        _cancellable.Cancel();
        var currentClosingContextId = string.Empty;
        try
        {
            foreach (var context in _contexts.ToArray())
            {
                currentClosingContextId = context.Id;
                context.Close();
            }
                
            _contexts.Clear();
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to close client context '{currentClosingContextId}' socket for {_name} while stopping because: {e.Message}", e);
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

    private void ProbeChannel()
    {
        if (IsStopped || !_canStartProbing)
        {
            return;
        }

        try
        {
            foreach (var context in _contexts.ToArray())
            {
                if (!context.IsClosed)
                {
                    if (context.Channel.Available > 0)
                    {
                        Read(context);
                    }
                    else
                    {
                        Write(context);
                    }
                }   
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Failed client channel processing for {_name} because: {e.Message}", e);
        }
    }

    private void Read(Context readable)
    {
        var channel = readable.Channel;

        // Create the state object.  
        var limit = readable.RequestBuffer.Limit();
        var state = new StateObject(channel, readable, new byte[limit], readable.RequestBuffer.Clear());
        if (state.Buffer != null)
        {
            channel.BeginReceive(state.Buffer, 0, (int)limit, 0, ReadCallback, state);   
        }
    }
        
    private void Write(Context writable)
    {
        var channel = writable.Channel;

        if (writable.HasNextWritable)
        {
            WriteWithCachedData(writable, channel);
        }
    }

    private void WriteWithCachedData(Context context, Socket channel)
    {
        for (var buffer = context.NextWritable(); buffer != null; buffer = context.NextWritable())
        {
            WriteWithCachedData(context, channel, buffer);
        }
    }

    private void WriteWithCachedData(Context context, Socket clientChannel, IConsumerByteBuffer buffer)
    {
        try
        {
            var responseBuffer = buffer.ToArray();
            var stateObject = new StateObject(clientChannel, context, null, buffer);
            // Begin sending the data to the remote device.  
            clientChannel.BeginSend(responseBuffer, 0, responseBuffer.Length, 0, SendCallback, stateObject); 
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to write buffer for {_name} with channel {clientChannel.RemoteEndPoint} because: {e.Message}", e);
        }
        finally
        {
            buffer.Release();
        }
    }

    private void ReadCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            var state = ar.AsyncState as StateObject;  
            var channel = state?.WorkSocket;
            var readable = state?.Context;
            var buffer = state?.ByteBuffer;
            var readBuffer = state?.Buffer!;
                
            // Read data from the client socket.   
            var bytesRead = channel?.EndReceive(ar);

            if (bytesRead.HasValue && bytesRead > 0)
            {
                buffer?.Put(readBuffer, 0, bytesRead.Value);
            }
                
            if (bytesRead == 0 && readable != null)
            {
                Close(readable.Channel, readable);
            }

            var bytesRemain = channel?.Available;
            if (bytesRemain > 0)
            {
                // Get the rest of the data.  
                channel?.BeginReceive(readBuffer,0,readBuffer.Length, 0, ReadCallback, state);
            }
            else
            {
                // All the data has arrived; put it in response.  
                if (buffer?.Limit() >= 1)
                {
                    readable?.Consumer.Consume(readable, buffer.Flip());
                }
                else
                {
                    buffer?.Release();
                }
            }
        }
        catch(Exception e)
        {
            // likely a forcible close by the client,
            // so force close and cleanup
            Logger.Error("Error while reading from the channel", e);
        }
    }
        
    private void AcceptCallback(IAsyncResult ar)
    {
        try
        {
            // Get the socket that handles the client request.  
            var listener = ar.AsyncState as Socket;
            var clientChannel = listener?.EndAccept(ar);
            if (clientChannel != null)
            {
                _contexts.Add(new Context(this, clientChannel));   
            }
        }
        catch (ObjectDisposedException e)
        {
            Logger.Error($"The underlying channel for {_name} is closed. This is certainly because Actor was stopped.", e);
        }
        finally
        {
            _canStartProbing = true;
        }
    }

    private void SendCallback(IAsyncResult ar)
    {  
        try
        {  
            // Retrieve the socket from the state object.  
            var state = ar.AsyncState as StateObject;
            var channel = state?.WorkSocket;

            // Complete sending the data to the remote device.  
            channel?.EndSend(ar);

        }
        catch (Exception e)
        {
            Logger.Error("Error while sending", e);
        }  
    }

    private class Context : RequestResponseContext
    {
        private readonly IConsumerByteBuffer _buffer;
        private readonly SocketChannelSelectionProcessorActor _parent;
        private readonly Socket _clientChannel;
        private object? _closingData;
        private readonly IRequestChannelConsumer _consumer;
        private object? _consumerData;
        private readonly string _id;
        private readonly Queue<IConsumerByteBuffer> _writables;
        private bool _requireExplicitClose;

        public Context(SocketChannelSelectionProcessorActor parent, Socket clientChannel)
        {
            _parent = parent;
            _clientChannel = clientChannel;
            _consumer = parent._provider.RequestChannelConsumer();
            _buffer = parent._requestBufferPool.Acquire();
            _id = $"{++_parent._contextId}";
            _writables = new Queue<IConsumerByteBuffer>();
            _requireExplicitClose = true;
        }

        public override T ConsumerData<T>() => (T) _consumerData!;

        public override T ConsumerData<T>(T workingData)
        {
            _consumerData = workingData;
            return workingData;
        }

        public override bool HasConsumerData => _consumerData != null;
            
        public override string Id => _id;
            
        public override IResponseSenderChannel Sender => _parent._responder;

        public override void WhenClosing(object data) => _closingData = data;

        public void Close()
        {
            if (_requireExplicitClose)
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
                _parent.Logger.Error($"Failed to close client channel for {_parent._name} because: {e.Message}", e);
            }
            finally
            {
                _buffer.Release();
                IsClosed = true;
            }
        }

        public IRequestChannelConsumer Consumer => _consumer;

        public override string? RemoteAddress()
        {
            try
            {
                return _clientChannel.RemoteEndPoint?.ToString();
            }
            catch (Exception)
            {
                _parent.Logger.Error("Unable to retrieve remote address");
                return string.Empty;
            }
        }

        public bool HasNextWritable => _writables.Count > 0;

        public IConsumerByteBuffer? NextWritable()
        {
            if (HasNextWritable)
            {
                return _writables.Dequeue();
            }

            return null;
        }

        public void QueueWritable(IConsumerByteBuffer buffer) => _writables.Enqueue(buffer);

        public void RequireExplicitClose(bool option) => _requireExplicitClose = option;

        public IConsumerByteBuffer RequestBuffer => _buffer;

        public Socket Channel => _clientChannel;
            
        public bool IsClosed { get; private set; }
    }
        
    private class StateObject
    {
        public StateObject(Socket workSocket, Context context, byte[]? buffer, IConsumerByteBuffer byteBuffer)
        {
            WorkSocket = workSocket;
            Context = context;
            Buffer = buffer;
            ByteBuffer = byteBuffer;
        }

        // Client socket.  
        public Socket WorkSocket { get; }
            
        // Receive buffer.  
        public byte[]? Buffer { get; }
            
        // Received data string.  
        public Context Context { get; }

        public IConsumerByteBuffer ByteBuffer { get; }
    }
}