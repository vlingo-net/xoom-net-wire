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

namespace Vlingo.Wire.Fdx.Outbound
{
    using Node;
    
    public class ManagedOutboundSocketChannel: IManagedOutboundChannel, IDisposable
    {
        private Socket _channel;
        private readonly Address _address;
        private readonly Node _node;
        private readonly ILogger _logger;
        private bool _disposed;

        public ManagedOutboundSocketChannel(Node node, Address address, ILogger logger)
        {
            _node = node;
            _address = address;
            _logger = logger;
        }
        
        public void Close()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                    Dispose(false);
                }
                catch (Exception e)
                {
                    _logger.Log($"Close of channel to {_node.Id} failed for because: {e.Message}", e);
                }
            }
        }

        public async void Write(Stream buffer)
        {
            _channel = await PreparedChannel();
            if (_channel == null)
            {
                return;
            }
            try
            {
                while (buffer.HasRemaining())
                {
                    var bytes = new byte[buffer.Length];
                    await buffer.ReadAsync(bytes, 0, bytes.Length);
                    await _channel.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                _logger.Log($"Write to {_node} failed because: {e.Message}", e);
                Close();
            }
        }

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
        }
        
        private async Task<Socket> PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.Poll(10000, SelectMode.SelectWrite))
                    {
                        return _channel;
                    }
                    
                    // Close();
                }
                
                var channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await channel.ConnectAsync(_address.HostName, _address.Port);
                return channel;
            }
            catch (Exception e)
            {
                Close();
                _logger.Log($"{GetType().Name}: Cannot prepare/open channel because: {e.Message}");
            }

            return null;
        }
    }
}
