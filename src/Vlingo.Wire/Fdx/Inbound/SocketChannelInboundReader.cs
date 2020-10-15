// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Inbound
{
    public class SocketChannelInboundReader: ChannelMessageDispatcher, IChannelReader, IDisposable
    {
        private readonly Socket _channel;
        private readonly List<Socket> _clientChannels;
        private bool _closed;
        private IChannelReaderConsumer? _consumer;
        private readonly ILogger _logger;
        private readonly int _maxMessageSize;
        private readonly string _name;
        private readonly int _port;
        private bool _disposed;
        private readonly ManualResetEvent _acceptDone;

        public SocketChannelInboundReader(int port, string name, int maxMessageSize, ILogger logger)
        {
            _port = port;
            _name = name;
            _maxMessageSize = maxMessageSize;
            _logger = logger;
            _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientChannels = new List<Socket>();
            _acceptDone = new ManualResetEvent(false);
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
                foreach (var clientChannel in _clientChannels.ToArray())
                {
                    clientChannel.Close();
                }
                Dispose(true);
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to close channel for: '{_name}'", e);
            }
        }

        public override string Name => _name;

        public int Port => _port;

        public void OpenFor(IChannelReaderConsumer consumer)
        {
            // for some tests it's possible to receive close() before start()
            if (_closed)
            {
                return;
            }

            _consumer = consumer;
            Logger.Debug($"{GetType().Name}: OPENING PORT: {_port}");
            _channel.Bind(new IPEndPoint(IPAddress.Any, _port));
            _channel.Listen(120);
        }

        public void ProbeChannel()
        {
            try
            {
                Accept();

                foreach (var clientChannel in _clientChannels.ToArray())
                {
                    if (clientChannel.Available > 0)
                    {
                        new SocketChannelSelectionReader(this, _logger).Read(clientChannel, new RawMessageBuilder(_maxMessageSize));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to read channel selector for: '{_name}' because: {e.Message}", e);
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

        public override IChannelReaderConsumer? Consumer => _consumer;

        public override ILogger Logger => _logger;
        
        //=========================================
        // internal implementation
        //=========================================
        
        private void Accept()
        {
            try
            {
                _channel.BeginAccept(AcceptCallback, _channel);
                _acceptDone.WaitOne();
            }
            catch (Exception e)
            {
                var message = $"Failed to accept client socket for {_name} because: {e.Message}";
                Logger.Error(message, e);
                throw;
            }
        }
        
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                var listener = ar.AsyncState as Socket;
                var clientChannel = listener?.EndAccept(ar);
                if (clientChannel != null)
                {
                    _clientChannels.Add(clientChannel);
                }
            }
            catch (ObjectDisposedException e)
            {
                Logger.Error(
                    $"The underlying channel for {_name} is closed. This is certainly because Actor was stopped.", e);
            }
            catch(Exception e)
            {
                _logger.Error($"{this}: Error occured on the underlying channel for {_name}", e);
            }
            finally
            {
                _acceptDone.Set();   
            }
        }
    }
}