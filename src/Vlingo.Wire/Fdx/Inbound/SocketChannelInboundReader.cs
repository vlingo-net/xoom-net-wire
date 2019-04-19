// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Inbound
{
    public class SocketChannelInboundReader: ChannelMessageDispatcher, IChannelReader, IDisposable
    {
        private readonly Socket _channel;
        private bool _closed;
        private IChannelReaderConsumer _consumer;
        private readonly ILogger _logger;
        private readonly int _maxMessageSize;
        private readonly string _name;
        private readonly int _port;
        private bool _disposed;
        private Socket _clientChannel;

        public SocketChannelInboundReader(int port, string name, int maxMessageSize, ILogger logger)
        {
            _port = port;
            _name = name;
            _maxMessageSize = maxMessageSize;
            _logger = logger;
            _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        
        //=========================================
        // InboundReader
        //=========================================
        
        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            try
            {
                _channel.Close();
                Dispose(true);
            }
            catch (Exception e)
            {
                _logger.Log($"Failed to close channel for: '{_name}'", e);
            }
        }

        public override string Name => _name;
        
        public void OpenFor(IChannelReaderConsumer consumer)
        {
            // for some tests it's possible to receive close() before start()
            if (_closed)
            {
                return;
            }

            _consumer = consumer;
            Logger.Log($"{GetType().Name}: OPENING PORT: {_port}");
            _channel.Bind(new IPEndPoint(IPAddress.Any, _port));
            _channel.Listen(120);
        }

        public async Task ProbeChannel()
        {
            try
            {
                if (_clientChannel == null)
                {
                    _clientChannel = await Accept(_channel);
                }
                else
                {
                    if (_clientChannel.Available > 0)
                    {
                        await new SocketChannelSelectionReader(this).Read(_clientChannel, new RawMessageBuilder(_maxMessageSize));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to read channel selector for: '{_name}' because: {e.Message}", e);
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
        
        //=========================================
        // ChannelMessageDispatcher
        //=========================================

        public override IChannelReaderConsumer Consumer => _consumer;

        public override ILogger Logger => _logger;
        
        //=========================================
        // internal implementation
        //=========================================
        
        private async Task<Socket> Accept(Socket channel)
        {
            try
            {
                if (channel.Poll(10000, SelectMode.SelectRead))
                {
                    return await channel.AcceptAsync();
                }
            }
            catch (Exception e)
            {
                var message = $"Failed to accept client socket for {_name} because: {e.Message}";
                Logger.Log(message, e);
                throw;
            }

            return null;
        }
    }
}