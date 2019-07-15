// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
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

        public int Write(RawMessage message, MemoryStream buffer)
        {
            buffer.Clear();
            message.CopyBytesTo(buffer);
            buffer.Flip();
            return Write(buffer);
        }

        public int Write(MemoryStream buffer)
        {
            _channel = PreparedChannel();
            
            var totalBytesWritten = 0;
            if (_channel == null)
            {
                return totalBytesWritten;
            }
            
            try
            {
                while (buffer.HasRemaining())
                {
                    var bytes = new byte[buffer.Length];
                    buffer.Read(bytes, 0, bytes.Length); // TODO: This could be async but is now blocking
                    totalBytesWritten += _channel.Send(bytes, SocketFlags.None); // TODO: This could be async but is now blocking
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

        private Socket PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.Poll(10000, SelectMode.SelectWrite))
                    {
                        return _channel;
                    }
                    
                    Close();
                }
                
                var channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                channel.Connect(_address.HostName, _address.Port); // TODO: This could be async but it is now blocking
                return channel;
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