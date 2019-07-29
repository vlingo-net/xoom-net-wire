// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public abstract class ClientRequestResponseChannel : IRequestSenderChannel, IResponseListenerChannel, IDisposable
    {
        protected readonly Address Address;
        protected readonly IResponseChannelConsumer Consumer;
        protected readonly ILogger Logger;
        protected ByteBufferPool ReadBufferPool;
        protected int PreviousPrepareFailures;
        
        private Socket _channel;
        private bool _closed;
        
        private bool _disposed;
        private readonly int _maxBufferPoolSize;
        private int _maxMessageSize;
        
        private readonly ManualResetEvent _sendDone;
        private readonly ManualResetEvent _receiveDone;

        public ClientRequestResponseChannel(
            Address address,
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            ILogger logger)
        {
            Address = address;
            Consumer = consumer;
            _maxBufferPoolSize = maxBufferPoolSize;
            _maxMessageSize = maxMessageSize;
            Logger = logger;
            _closed = false;
            _channel = null;
            PreviousPrepareFailures = 0;
            _sendDone = new ManualResetEvent(false);
            _receiveDone = new ManualResetEvent(false);
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
            Dispose(true);
        }

        public void RequestWith(byte[] buffer)
        {
            Socket preparedChannel = null;
            while (preparedChannel == null && PreviousPrepareFailures < 10)
            {
                preparedChannel = PreparedChannel();
            }

            if (preparedChannel != null)
            {
                try
                {
                    preparedChannel.BeginSend(buffer, 0, buffer.Length, 0, SendCallback, preparedChannel);
                    _sendDone.WaitOne();
                }
                catch (Exception e)
                {
                    Logger.Error($"Write to socket failed because: {e.Message}", e);
                    CloseChannel();
                }
            }
        }

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
                while (channel == null && PreviousPrepareFailures < 10)
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
                Logger.Error($"Failed to read channel selector for {Address} because: {e.Message}", e);
            }
        }
        
        //=========================================
        // Dispose
        //=========================================
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
      
            if (disposing) 
            {
                Close();
            }
      
            _disposed = true;
            _receiveDone.Dispose();
            _sendDone.Dispose();
        }

        //=========================================
        // internal implementation
        //=========================================

        protected Socket Channel => _channel;

        protected int MaxMessageSize
        {
            get => _maxMessageSize;
            set => _maxMessageSize = value;
        }

        protected void CloseChannel()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to close channel to {Address} because: {e.Message}", e);
                }
            }

            _channel = null;
        }
        
        protected abstract Socket PreparedChannelDelegate();

        private Socket PreparedChannel()
        {
            var prepared = PreparedChannelDelegate();
            _channel = prepared;
            return prepared;
        }

        private void ReadConsume(Socket channel)
        {
            var pooledBuffer = PooledByteBuffer();
            var readBuffer = pooledBuffer.ToArray();
            try
            {
                // Create the state object.  
                StateObject state = new StateObject(channel, readBuffer, pooledBuffer);
                channel.BeginReceive(readBuffer, 0, readBuffer.Length, 0, ReceiveCallback, state);
                _receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                Logger.Error("Cannot begin receiving on the channel", e);
                throw;
            }
        }
        
        private IConsumerByteBuffer PooledByteBuffer()
        {
            if (ReadBufferPool == null)
            {
                ReadBufferPool = new ByteBufferPool(_maxBufferPoolSize, _maxMessageSize);
            }
            return ReadBufferPool.AccessFor("client-response", 25);
        }

        /*private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Logger.Info($"Socket connected to {client.RemoteEndPoint}");

                // Signal that the connection has been made.  
                _connectDone.Set();
            }
            catch (Exception e)
            {
                Logger.Error("Cannot connect", e);
            }
        }*/

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                client.EndSend(ar);

                // Signal that all bytes have been sent.  
                _sendDone.Set();
            }
            catch (Exception e)
            {
                Logger.Error("Error while sending bytes", e);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            var state = (StateObject)ar.AsyncState;
            var client = state.WorkSocket;
            var pooledBuffer = state.PooledByteBuffer;
            var readBuffer = state.Buffer;

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
                    client.BeginReceive(readBuffer,0,readBuffer.Length,0, ReceiveCallback, state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (pooledBuffer.Limit() >= 1)
                    {
                        Consumer.Consume(pooledBuffer.Flip());
                    }
                    else
                    {
                        pooledBuffer.Release();
                    }

                    // Signal that all bytes have been received.  
                    _receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                pooledBuffer.Release();
                Logger.Error("Error while receiving bytes", e);
                throw;
            }
        }

        private class StateObject
        {
            public StateObject(Socket workSocket, byte[] buffer, IConsumerByteBuffer pooledByteBuffer)
            {
                WorkSocket = workSocket;
                Buffer = buffer;
                PooledByteBuffer = pooledByteBuffer;
            }
            
            public Socket WorkSocket { get; }

            public byte[] Buffer { get; }

            public IConsumerByteBuffer PooledByteBuffer { get; }
        }
    }
}