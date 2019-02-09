// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class ClientRequestResponseChannelPipeline : IRequestSenderChannel, IResponseListenerChannel
    {
        private readonly Address _address;
        private Socket _channel;
        private bool _closed;
        private readonly IResponseChannelConsumer _consumer;
        private readonly ILogger _logger;
        private int _previousPrepareFailures;

        public ClientRequestResponseChannelPipeline(
            Address address,
            IResponseChannelConsumer consumer,
            ILogger logger)
        {
            _address = address;
            _consumer = consumer;
            _logger = logger;
            _closed = false;
            _channel = null;
            _previousPrepareFailures = 0;
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

        public async Task RequestWithAsync(NetworkStream stream)
        {
            var preparedChannel = await PreparedChannelAsync();
            if (preparedChannel != null)
            {
                try
                {
                    while (stream.HasRemaining())
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent(1024);
                        await stream.ReadAsync(buffer, 0, buffer.Length);
                        await preparedChannel.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        ArrayPool<byte>.Shared.Return(buffer);
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
                    var pipe = new Pipe();
                    var input = ReadConsumeAsync(channel, pipe.Writer);
                    var output = WriteConsumeAsync(channel, pipe.Reader);
                    await Task.WhenAll(input, output);
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
                    _channel.Blocking = false;
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

        private async Task ReadConsumeAsync(Socket channel, PipeWriter writer)
        {
            const int minimumBufferSize = 512;
            while (true)
            {
                try
                {
                    // Request a minimum of 512 bytes from the PipeWriter
                    var memory = writer.GetMemory(minimumBufferSize);
                    var bytesRead = await channel.ReceiveAsync(memory, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    
                    // Tell the PipeWriter how much was read
                    writer.Advance(bytesRead);
                }
                catch (Exception)
                {
                    break;
                }
                
                // Make the data available to the PipeReader
                var result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }
            
            // Signal to the reader that we're done writing
            writer.Complete();
        }

        private async Task WriteConsumeAsync(Socket channel, PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;
                _consumer.Consume(buffer);
                
                // We sliced the buffer until no more data could be processed
                // Tell the PipeReader how much we consumed and how much we left to process
                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
            
            reader.Complete();
        }
    }
}