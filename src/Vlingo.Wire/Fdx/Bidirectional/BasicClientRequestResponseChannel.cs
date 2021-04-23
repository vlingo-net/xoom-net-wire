// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Xoom.Common.Pool;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;
using Vlingo.Xoom.Actors;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class BasicClientRequestResponseChannel : IClientRequestResponseChannel, IDisposable
    {
        private readonly Address _address;
        private Socket? _channel;
        private bool _closed;
        private readonly IResponseChannelConsumer _consumer;
        private readonly ILogger _logger;
        private int _previousPrepareFailures;
        private readonly ConsumerByteBufferPool _readBufferPool;
        private bool _disposed;
        private bool _canStartProbing;
        private readonly AutoResetEvent _connectDone;

        public BasicClientRequestResponseChannel(
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
            _readBufferPool = new ConsumerByteBufferPool(ElasticResourcePool<IConsumerByteBuffer, string>.Config.Of(maxBufferPoolSize), maxMessageSize);
            _connectDone = new AutoResetEvent(false);
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
            Socket? preparedChannel = null;
            while (preparedChannel == null && _previousPrepareFailures < 10)
            {
                preparedChannel = PreparedChannel();
            }

            if (preparedChannel != null)
            {
                try
                {
                    preparedChannel.BeginSend(buffer, 0, buffer.Length, 0, SendCallback, preparedChannel);
                }
                catch (Exception e)
                {
                    _logger.Error($"Write to socket failed because: {e.Message}", e);
                    CloseChannel();
                }
            }
        }

        //=========================================
        // ResponseListenerChannel
        //=========================================

        public void ProbeChannel()
        {
            if (_closed || !_canStartProbing)
            {
                return;
            }

            try
            {
                Socket? channel = null;
                while (channel == null && _previousPrepareFailures < 10)
                {
                    channel = PreparedChannel();
                }
                if (channel != null && channel.Available > 0)
                {
                    ReadConsume(channel);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to read channel selector for {_address} because: {e.Message}", e);
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
                
                _connectDone.Dispose();
            }

            _disposed = true;
        }

        //=========================================
        // internal implementation
        //=========================================

        private void CloseChannel()
        {
            if (_closed)
            {
                return;
            }
            
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                }
                catch (Exception e)
                {
                    _logger.Error($"Failed to close channel to {_address} because: {e.Message}", e);
                }
            }

            _channel = null;
        }

        private Socket? PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    _previousPrepareFailures = 0;
                    return _channel;
                }

                _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _channel.BeginConnect(_address.HostName, _address.Port, ConnectCallback, _channel);
                if (!_connectDone.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("BasicClientRequestResponseChannel timeout of 5s for connection achieved");
                }
                _previousPrepareFailures = 0;
                return _channel;
            }
            catch (Exception e)
            {
                CloseChannel();
                var message = $"{GetType().Name}: Cannot prepare/open channel because: {e.Message}";
                if (_previousPrepareFailures == 0)
                {
                    _logger.Error(message, e);
                }
                else if (_previousPrepareFailures % 20 == 0)
                {
                    _logger.Info($"AGAIN: {message}");
                }
            }
            ++_previousPrepareFailures;
            return null;
        }
        
        private void ReadConsume(Socket channel)
        {
            IConsumerByteBuffer? pooledBuffer = null;
            try
            {
                pooledBuffer = _readBufferPool.Acquire();
                var readBuffer = pooledBuffer.ToArray();
                // Create the state object.  
                var state = new StateObject(channel, readBuffer, pooledBuffer);
                channel.BeginReceive(readBuffer, 0, readBuffer.Length, 0, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                if (pooledBuffer != null)
                {
                    pooledBuffer.Release();
                    throw;
                }
                
                throw new InvalidOperationException($"No pooled buffers remaining: {e.Message}", e);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = ar.AsyncState as Socket;

                // Complete the connection.  
                client?.EndConnect(ar);

                _logger.Info($"Socket connected to {client?.RemoteEndPoint}");
                
                // Signal that the connection has been made.  
                _connectDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error("Cannot connect", e);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = ar.AsyncState as Socket;

                // Complete sending the data to the remote device.  
                client?.EndSend(ar);
            }
            catch (Exception e)
            {
                _logger.Error("Error while sending bytes", e);
            }
            finally
            {
                _canStartProbing = true;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (_closed || _disposed)
            {
                _logger.Error("The underlying socket is already disposed but there is still an ongoing receive callback");
                return;
            }
            
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            var state = ar.AsyncState as StateObject;
            var client = state?.WorkSocket;
            var pooledBuffer = state?.PooledByteBuffer;
            var readBuffer = state?.Buffer;

            try
            {
                // Read data from the remote device.  
                var bytesRead = client?.EndReceive(ar);

                if (bytesRead.HasValue && bytesRead > 0 && readBuffer != null)
                {
                    // There might be more data, so store the data received so far.  
                    pooledBuffer?.Put(readBuffer, 0, bytesRead.Value);
                }

                var bytesRemain = client?.Available;
                if (bytesRemain.HasValue && bytesRemain > 0 && readBuffer != null)
                {
                    // Get the rest of the data.  
                    client?.BeginReceive(readBuffer,0,readBuffer.Length,0, ReceiveCallback, state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (pooledBuffer?.Limit() >= 1)
                    {
                        _consumer.Consume(pooledBuffer.Flip());
                    }
                    else
                    {
                        pooledBuffer?.Release();
                    }
                }
            }
            catch (Exception e)
            {
                pooledBuffer?.Release();
                _logger.Error("Error while receiving bytes", e);
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