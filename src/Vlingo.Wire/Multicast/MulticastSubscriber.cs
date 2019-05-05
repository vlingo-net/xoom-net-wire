// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Multicast
{
    public class MulticastSubscriber : ChannelMessageDispatcher, IChannelReader, IDisposable
    {
        private readonly MemoryStream _buffer;
        private bool _closed;
        private readonly Socket _channel;
        private IChannelReaderConsumer _consumer;
        private readonly IPAddress _groupAddress;
        private readonly ILogger _logger;
        private readonly int _maxReceives;
        private readonly RawMessage _message;
        private readonly string _name;
        private NetworkInterface _networkInterface;
        private readonly EndPoint _ipEndPoint;
        private bool _disposed;

        public MulticastSubscriber(
            string name,
            Group group,
            int maxMessageSize,
            int maxReceives,
            ILogger logger) : this(name, group, null, maxMessageSize, maxReceives, logger)
        {
        }

        public MulticastSubscriber(
            string name,
            Group group,
            string networkInterfaceName,
            int maxMessageSize,
            int maxReceives,
            ILogger logger)
        {
            _name = name;
            _maxReceives = maxReceives;
            _logger = logger;
            _channel = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _channel.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _channel.ExclusiveAddressUse = false;
            _channel.Blocking = false;
            _ipEndPoint = new IPEndPoint(IPAddress.Any, group.Port);
            _channel.Bind(_ipEndPoint);
            _networkInterface = AssignNetworkInterfaceTo(_channel, networkInterfaceName);
            _groupAddress = IPAddress.Parse(group.Address);

            var p = _networkInterface.GetIPProperties().GetIPv4Properties();
            var mcastOption = new MulticastOption(_groupAddress, p.Index);
            _channel.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
            _channel.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 50);
            
            _buffer = new MemoryStream(maxMessageSize);
            _message = new RawMessage(maxMessageSize);
            
            logger.Log($"MulticastSubscriber joined: {_networkInterface.Id}");
        }
        
        //=========================================
        // ChannelMessageDispatcher
        //=========================================

        public override IChannelReaderConsumer Consumer => _consumer;
        
        public override ILogger Logger => _logger;
        
        //=========================================
        // ChannelReader
        //=========================================
        
        public override string Name => _name;

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
                _buffer.Dispose();
                Dispose(true);
            }
            catch (Exception e)
            {
                _logger.Log($"Failed to close channel for: '{_name}'", e);
            }
        }

        public void OpenFor(IChannelReaderConsumer consumer)
        {
            if (_closed)
            {
                return;
            }

            _consumer = consumer;
        }

        public async Task ProbeChannel()
        {
            if (_closed)
            {
                return;
            }

            try
            {
                // when nothing is received, receives represents retries
                // and possibly some number of receives
                for (var receives = 0; receives < _maxReceives; ++receives)
                {
                    _buffer.SetLength(0); // clear
                    var bytes = new byte [_buffer.Capacity];
                    // check for availability because otherwise surprisingly
                    // the call to _channel.ReceiveFromAsync is blocking and
                    // _channel.Blocking = false; is not taken into account
                    if (_channel.Available > 0)
                    {
                        var received = await _channel.ReceiveFromAsync(
                            new ArraySegment<byte>(bytes),
                            SocketFlags.None,
                            _ipEndPoint);
                        
                        if (received.ReceivedBytes > 0)
                        {
                            _buffer.Write(bytes, 0, received.ReceivedBytes);
                            _buffer.Flip();
                            _message.From(_buffer);
                        
                            _consumer.Consume(_message);
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                _logger.Log($"Failed to read channel selector for: '{_name}'", e);
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
        // internal implementation
        //=========================================
        
        private NetworkInterface AssignNetworkInterfaceTo(Socket channel, string networkInterfaceName)
        {
            if (networkInterfaceName != null && networkInterfaceName.Trim() != string.Empty)
            {
                var specified = NetworkInterface.GetAllNetworkInterfaces()
                    .SingleOrDefault(ni => ni.Name == networkInterfaceName);

                if (specified != null)
                {
                    var p = specified.GetIPProperties().GetIPv4Properties();
                    channel.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(p.Index));
                }
            }

            // if networkInterfaceName not given or unknown, take best guess
            return AssignBestGuessNetworkInterfaceTo(channel);
        }

        private NetworkInterface AssignBestGuessNetworkInterfaceTo(Socket channel)
        {
            NetworkInterface networkInterface = null;
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var candidate in networkInterfaces)
            {
                var candidateName = candidate.Name.ToLowerInvariant();
                if (!candidateName.Contains("virtual") && !candidateName.StartsWith("v"))
                {
                    if (candidate.OperationalStatus == OperationalStatus.Up &&
                        candidate.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        candidate.NetworkInterfaceType != NetworkInterfaceType.Ppp /* lacks isVirtual() */)
                    {
                        try
                        {
                            var p = candidate.GetIPProperties().GetIPv4Properties();
                            channel.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(p.Index));
                            networkInterface = candidate;
                            break;
                        }
                        catch (Exception)
                        {
                            networkInterface = null;
                        }
                    }
                }
            }
            
            if (networkInterface == null)
            {
                throw new IOException("Cannot assign network interface");
            }

            return networkInterface;
        }
    }
}