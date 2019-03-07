// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class ClientRequestResponseChannel : IRequestSenderChannel, IResponseListenerChannel
    {
        private readonly Address _address;
        private Socket _channel;
        private bool _closed;
        private readonly IResponseChannelConsumer _consumer;
        private readonly ILogger _logger;
        private int _previousPrepareFailures;
        private readonly ByteBufferPool _readBufferPool;

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

        public async Task RequestWithAsync(Stream stream)
        {
            var preparedChannel = await PreparedChannelAsync();
            if (preparedChannel != null)
            {
                try
                {
                    while (stream.HasRemaining())
                    {
                        var buffer = new byte[stream.Length];
                        await stream.ReadAsync(buffer, 0, buffer.Length);
                        await preparedChannel.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    }
                }
                catch (Exception e)
                {
                    _logger.Log($"Write to socket failed because: {e.Message}", e);
                    CloseChannel();
                }
            }
        }
        
        //=========================================
        // ResponseListenerChannel
        //=========================================

        public async Task ProbeChannelAsync()
        {
            if (_closed)
            {
                return;
            }

            try
            {
                var channel = await PreparedChannelAsync();
                if (channel != null)
                {
                    await ReadConsumeAsync(channel);
                }
            }
            catch (Exception e)
            {
                _logger.Log($"Failed to read channel selector for {_address} because: {e.Message}", e);
            }
        }
        
        //=========================================
        // internal implementation
        //=========================================

        private void CloseChannel()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                }
                catch (Exception e)
                {
                    _logger.Log($"Failed to close channel to {_address} because: {e.Message}", e);
                }
            }

            _channel = null;
        }

        private async Task<Socket> PreparedChannelAsync()
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
                    _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await _channel.ConnectAsync(_address.HostName, _address.Port);
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

        private async Task ReadConsumeAsync(Socket channel)
        {
            var pooledBuffer = _readBufferPool.AccessFor("client-response", 25);
            var readBuffer = pooledBuffer.ToArray();
            var totalBytesRead = 0;
            var bytesRead = 0;
            try
            {
                do
                {
                    bytesRead = await channel.ReceiveAsync(readBuffer, SocketFlags.None);
                    pooledBuffer.Put(readBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                } while (channel.Available > 0);

                if (totalBytesRead > 0)
                {
                    _consumer.Consume(pooledBuffer.Flip());
                }
                else
                {
                    pooledBuffer.Release();
                }
            }
            catch (Exception)
            {
                pooledBuffer.Release();
                throw;
            }
        }
    }
}