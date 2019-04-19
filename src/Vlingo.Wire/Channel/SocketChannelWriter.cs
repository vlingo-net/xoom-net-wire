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
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Channel
{
    public class SocketChannelWriter
    {
        private Socket _channel;
        private readonly Address _address;
        private readonly ILogger _logger;

        public SocketChannelWriter(Address address, ILogger logger)
        {
            _address = address;
            _logger = logger;
            _channel = null;
        }
        
        public void Close() 
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                } 
                catch (Exception e)
                {
                    _logger.Log($"Channel close failed because: {e.Message}", e);
                }
            }
            
            _channel = null;
        }

        public async Task<int> Write(RawMessage message, MemoryStream buffer)
        {
            buffer.Clear();
            message.CopyBytesTo(buffer);
            buffer.Flip();
            return await Write(buffer);
        }

        public async Task<int> Write(MemoryStream buffer)
        {
            var preparedChannel = await PreparedChannel();
            var totalBytesWritten = 0;
            try
            {
                while (buffer.HasRemaining())
                {
                    var bytes = new byte[buffer.Length];
                    await buffer.ReadAsync(bytes, 0, bytes.Length);
                    totalBytesWritten += await preparedChannel.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                _logger.Log($"Write to channel failed because: {e.Message}", e);
                Close();
            }

            return totalBytesWritten;
        }

        public override string ToString() => $"SocketChannelWriter[address={_address}, channel={_channel}]";

        private async Task<Socket> PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.IsSocketConnected())
                    {
                        return _channel;
                    }
                    
                    Close();
                }
                else
                {
                    _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await _channel.ConnectAsync(_address.HostName, _address.Port);
                    return _channel;
                }
            }
            catch (Exception e)
            {
                _logger.Log($"{this}: Failed to prepare channel because: {e.Message}", e);
                Close();
            }

            return null;
        }
    }
}