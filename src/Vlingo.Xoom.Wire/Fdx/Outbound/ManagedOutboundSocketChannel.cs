// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Outbound
{
    public class ManagedOutboundSocketChannel: IManagedOutboundChannel, IDisposable
    {
        private Socket? _channel;
        private readonly Address _address;
        private readonly Node _node;
        private readonly ILogger _logger;
        private bool _disposed;
        private readonly SemaphoreSlim _connectAtOnce;
        private readonly ManualResetEvent _connectDone;

        public ManagedOutboundSocketChannel(Node node, Address address, ILogger logger)
        {
            _node = node;
            _address = address;
            _logger = logger;
            _connectAtOnce = new SemaphoreSlim(1);
            _connectDone = new ManualResetEvent(false);
        }
        
        public void Close()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                    _connectDone.Reset();
                    Dispose(false);
                }
                catch (Exception e)
                {
                    _logger.Error($"Close of channel to {_node.Id} failed for because: {e.Message}", e);
                }
            }
        }

        public void Write(Stream buffer)
        {
            _channel = PreparedChannel();
            if (_channel == null)
            {
                return;
            }
            try
            {
                while (buffer.HasRemaining())
                {
                    if (!_connectDone.WaitOne(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException("ManagedOutboundSocketChannel timeout of 5s for connection achieved");
                    }
                    
                    var bytes = new byte[buffer.Length];
                    buffer.Read(bytes, 0, bytes.Length); // TODO: can be done async
                    _channel.BeginSend(bytes, 0, bytes.Length, 0, SendCallback, _channel);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Write to {_node} failed because: {e.Message}", e);
                _connectDone.Set();
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
                
                _connectAtOnce.Dispose();
                _connectDone.Dispose();
            }
      
            _disposed = true;
        }
        
        private Socket? PreparedChannel()
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
                
                _connectAtOnce.Wait();
                var channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                channel.BeginConnect(_address.HostName, _address.Port, ConnectCallback, channel);
                return channel;
            }
            catch (Exception e)
            {
                _logger.Error($"{GetType().Name}: Cannot prepare/open channel because: {e.Message}");
                _connectAtOnce.Release();
                Close();
            }

            return null;
        }
        
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = ar.AsyncState as Socket;

                // Complete the connection.  
                client?.EndConnect(ar);

                _logger.Debug($"Socket connected to {client?.RemoteEndPoint}");
            }
            catch (Exception e)
            {
                _logger.Error("Cannot connect", e);
            }
            finally
            {
                // Signal that the connection has been made.  
                _connectAtOnce.Release();
                _connectDone.Set();
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
        }
    }
}