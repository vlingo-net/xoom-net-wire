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
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    using System.Text;
    using System.Threading;

    using Vlingo.Common.Message;

    public class ClientRequestResponseChannel : IRequestSenderChannel, IResponseListenerChannel //, IMessageQueueListener
    {
        private readonly Address _address;
        private Socket _channel;
        private bool _closed;
        private readonly IResponseChannelConsumer _consumer;
        private readonly ILogger _logger;
        private int _previousPrepareFailures;
        private readonly ByteBufferPool _readBufferPool;
        private readonly AsyncMessageQueue _consumerQueue;
        private readonly AsyncMessageQueue _sendQueue;

        // ManualResetEvent instances signal completion.  
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        public ClientRequestResponseChannel(
            Address address,
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            ILogger logger)
        {
            _address = address;
            _consumer = consumer;
            _logger = logger;
            _closed = false;
            _channel = null;
            _previousPrepareFailures = 0;
            _readBufferPool = new ByteBufferPool(maxBufferPoolSize, maxMessageSize);
            // _consumerQueue = new AsyncMessageQueue();
            // _consumerQueue.RegisterListener(new ConsumerBufferListener(consumer));
            // _sendQueue = new AsyncMessageQueue();
            // _sendQueue.RegisterListener(this);

        }
        
        //=========================================
        // RequestSenderChannel
        //=========================================
        
        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            CloseChannel();
        }

        public void RequestWith(byte[] buffer)
        {
            //_sendQueue.Enqueue(new SendMessage(buffer));
            Socket preparedChannel = null;
            while (preparedChannel == null && _previousPrepareFailures < 10)
            {
                preparedChannel = PreparedChannel();
            }

            if (preparedChannel != null)
            {
                try
                {
                    // var buffer = ((SendMessage)message).Buffer;
                    _logger.Log("SENDING from client: " + buffer.BytesToText(0, buffer.Length) + " | " + buffer.Length);
                    preparedChannel.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), preparedChannel);
                    sendDone.WaitOne();
                }
                catch (Exception e)
                {
                    _logger.Log($"Write to socket failed because: {e.Message}", e);
                    CloseChannel();
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                //_logger.Log($"Sent {bytesSent} bytes to server.");

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                _logger.Log("SendCallback", e);
            }
        }

        //public async void RequestWith(byte[] buffer)
        //{
        //    //_sendQueue.Enqueue(new SendMessage(buffer));
        //    Socket preparedChannel = null;
        //    while (preparedChannel == null && _previousPrepareFailures < 10)
        //    {
        //        preparedChannel = await PreparedChannel();
        //    }

        //    if (preparedChannel != null)
        //    {
        //        try
        //        {
        //            // var buffer = ((SendMessage)message).Buffer;
        //            _logger.Log("SENDING from client: " + buffer.BytesToText(0, buffer.Length) + " | " + buffer.Length);
        //            await preparedChannel.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
        //            await Task.Delay(1);
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.Log($"Write to socket failed because: {e.Message}", e);
        //            CloseChannel();
        //        }
        //    }
        //}

        //=========================================
        // ResponseListenerChannel
        //=========================================

        public void ProbeChannel()
        {
            if (_closed)
            {
                return;
            }

            try
            {
                Socket channel = null;
                while (channel == null && _previousPrepareFailures < 10)
                {
                    channel = PreparedChannel();
                }
                if (channel != null)
                {
                    ReadConsume(channel);
                }
            }
            catch (Exception e)
            {
                _logger.Log($"Failed to read channel selector for {_address} because: {e.Message}", e);
            }
        }

        //public async void ProbeChannel()
        //{
        //    if (_closed)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        Socket channel = null;
        //        while (channel == null && _previousPrepareFailures < 10)
        //        {
        //            channel = PreparedChannel();
        //        }
        //        if (channel != null)
        //        {
        //            await ReadConsume(channel);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.Log($"Failed to read channel selector for {_address} because: {e.Message}", e);
        //    }
        //}
        
        //=========================================
        // internal implementation
        //=========================================

        private void CloseChannel()
        {
            if (_channel != null)
            {
                try
                {
                    _logger.Log("CLOSING Client Channel");
                    _channel.Close();
                    connectDone.Dispose();
                    receiveDone.Dispose();
                    sendDone.Dispose();
                }
                catch (Exception e)
                {
                    _logger.Log($"Failed to close channel to {_address} because: {e.Message}", e);
                }
            }

            _channel = null;
        }

        private Socket PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.IsSocketConnected())
                    {
                        _previousPrepareFailures = 0;
                        return _channel;
                    }

                    CloseChannel();
                }
                else
                {
                    _logger.Log("CONNECTING");
                    _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _channel.BeginConnect(_address.HostName, _address.Port, ConnectCallback, _channel);
                    connectDone.WaitOne();
                    _previousPrepareFailures = 0;
                    return _channel;
                }
            }
            catch (Exception e)
            {
                CloseChannel();
                var message = $"{GetType().Name}: Cannot prepare/open channel because: {e.Message}";
                if (_previousPrepareFailures == 0)
                {
                    _logger.Log(message, e);
                }
                else if (_previousPrepareFailures % 20 == 0)
                {
                    _logger.Log($"AGAIN: {message}");
                }
            }
            ++_previousPrepareFailures;
            return null;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                _logger.Log($"Socket connected to {client.RemoteEndPoint}");

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                _logger.Log("Cannot connect", e);
            }
        }

        //private async Task<Socket> PreparedChannel()
        //{
        //    try
        //    {
        //        if (_channel != null)
        //        {
        //            if (_channel.IsSocketConnected())
        //            {
        //                _previousPrepareFailures = 0;
        //                return _channel;
        //            }

        //            // CloseChannel();
        //        }
        //        else
        //        {
        //            _logger.Log("CONNECTING");
        //            _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //            await _channel.ConnectAsync(_address.HostName, _address.Port);
        //            _previousPrepareFailures = 0;
        //            return _channel;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        CloseChannel();
        //        var message = $"{GetType().Name}: Cannot prepare/open channel because: {e.Message}";
        //        if (_previousPrepareFailures == 0)
        //        {
        //            _logger.Log(message, e);
        //        }
        //        else if (_previousPrepareFailures % 20 == 0)
        //        {
        //            _logger.Log($"AGAIN: {message}");
        //        }
        //    }
        //    ++_previousPrepareFailures;
        //    return null;
        //}

        //private async Task ReadConsume(Socket channel)
        //{
        //    var pooledBuffer = _readBufferPool.AccessFor("client-response", 25);
        //    var readBuffer = pooledBuffer.ToArray();
        //    var totalBytesRead = 0;
        //    var bytesRead = 0;
        //    try
        //    {
        //        do
        //        {
        //            bytesRead = await channel.ReceiveAsync(readBuffer, SocketFlags.None);
        //            pooledBuffer.Put(readBuffer, 0, bytesRead);

        //            totalBytesRead += bytesRead;
        //        } while (channel.Available > 0);

        //        if (totalBytesRead > 0)
        //        {
        //            _logger.Log("RECEIVED on CLIENT: " + readBuffer.BytesToText(0, bytesRead) + " | " + totalBytesRead);
        //            //_consumerQueue.Enqueue(new ConsumerBuffer(pooledBuffer.Flip()));
        //            //lock (locking)
        //            //{
        //                _consumer.Consume(pooledBuffer.Flip());
        //            //}
        //        }
        //        else
        //        {
        //            pooledBuffer.Release();
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        pooledBuffer.Release();
        //        throw;
        //    }
        //}

        private void ReadConsume(Socket channel)
        {
            var pooledBuffer = _readBufferPool.AccessFor("client-response", 25);
            var readBuffer = pooledBuffer.ToArray();
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = channel;
                state.PooledByteBuffer = pooledBuffer;
                state.buffer = readBuffer;

                channel.BeginReceive(readBuffer, 0, readBuffer.Length, 0, new AsyncCallback(ReceiveCallback), state);
                receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                _logger.Log("ReadConsume", e);
                throw;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            var pooledBuffer = state.PooledByteBuffer;
            var readBuffer = state.buffer;

            try
            {
                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    pooledBuffer.Put(readBuffer, 0, bytesRead);
                }

                int bytesRemain = client.Available;
                if (bytesRemain > 0)
                {
                    // Get the rest of the data.  
                    client.BeginReceive(
                        readBuffer,
                        0,
                        readBuffer.Length,
                        0,
                        new AsyncCallback(ReceiveCallback),
                        state);
                }
                else
                {
                    _logger.Log("RECEIVED on CLIENT: " + readBuffer.BytesToText(0, bytesRead) + " | " + pooledBuffer.Limit());
                    // All the data has arrived; put it in response.  
                    if (pooledBuffer.Limit() >= 1)
                    {
                        _consumer.Consume(pooledBuffer.Flip());
                    }
                    else
                    {
                        pooledBuffer.Release();
                    }

                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                pooledBuffer.Release();
                _logger.Log("ReceiveCallback", e);
                throw;
            }
        }

        //public async void HandleMessage(IMessage message)
        //{
        //    Socket preparedChannel = null;
        //    while (preparedChannel == null && _previousPrepareFailures < 10)
        //    {
        //        preparedChannel = await PreparedChannel();
        //    }

        //    if (preparedChannel != null)
        //    {
        //        try
        //        {
        //            var buffer = ((SendMessage)message).Buffer;
        //            _logger.Log("SENDING from client: " + buffer.BytesToText(0, buffer.Length) + " | " + buffer.Length);
        //            await preparedChannel.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.Log($"Write to socket failed because: {e.Message}", e);
        //            CloseChannel();
        //        }
        //    }
        //}
    }

    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer;
        // Received data string.  
        public ByteBufferPool.PooledByteBuffer PooledByteBuffer;
    }

    //public class SendMessage : IMessage
    //{
    //    public byte[] Buffer { get; }

    //    public SendMessage(byte[] buffer)
    //    {
    //        this.Buffer = buffer;
    //    }
    //}

    //public class ConsumerBufferListener : IMessageQueueListener
    //{
    //    private readonly IResponseChannelConsumer _consumer;

    //    public ConsumerBufferListener(IResponseChannelConsumer consumer)
    //    {
    //        _consumer = consumer;
    //    }

    //    public void HandleMessage(IMessage message)
    //    {
    //        _consumer.Consume(((ConsumerBuffer)message).Buffer);
    //    }
    //}

    //public class ConsumerBuffer : IMessage
    //{
    //    public IConsumerByteBuffer Buffer { get; }

    //    public ConsumerBuffer(IConsumerByteBuffer buffer)
    //    {
    //        Buffer = buffer;
    //    }
    //}
}