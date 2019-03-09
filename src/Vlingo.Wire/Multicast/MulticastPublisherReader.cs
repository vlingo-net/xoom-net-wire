// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Multicast
{
    public class MulticastPublisherReader : ChannelMessageDispatcher, IChannelPublisher, IDisposable
    {
        private readonly RawMessage _availability;
        private readonly Socket _publisherChannel;
        private bool _closed;
        private readonly IChannelReaderConsumer _consumer;
        private readonly EndPoint _groupAddress;
        private readonly ILogger _logger;
        private readonly MemoryStream _messageBuffer;
        private readonly Queue<RawMessage> _messageQueue;
        private readonly string _name;
        private readonly IPEndPoint _publisherAddress;
        private readonly Socket _readChannel;
        private bool _disposed;

        public MulticastPublisherReader(
            string name,
            Group group,
            int incomingSocketPort,
            int maxMessageSize,
            IChannelReaderConsumer consumer,
            ILogger logger)
        {
            _name = name;
            _consumer = consumer;
            _logger = logger;
            _groupAddress = new IPEndPoint(IPAddress.Parse(group.Address), group.Port);
            _messageBuffer = new MemoryStream(maxMessageSize);
            _messageQueue = new Queue<RawMessage>();
            
            _publisherChannel = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _publisherChannel.Blocking = false;
            _publisherChannel.ExclusiveAddressUse = false;
            // binds to an assigned local address that is
            // published as my availabilityMessage
            _publisherChannel.Bind(new IPEndPoint(IPAddress.Any, 0));
            
            _readChannel = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _readChannel.Blocking = false;
            _readChannel.ExclusiveAddressUse = false;
            _readChannel.Bind(new IPEndPoint(IPAddress.Any, incomingSocketPort));
            
            _publisherAddress = (IPEndPoint)_readChannel.LocalEndPoint;
            
            _availability = AvailabilityMessage();
        }
        
        //====================================
        // ChannelPublisher
        //====================================

        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            try
            {
                _publisherChannel.Close();
                _messageBuffer.Dispose();
                _readChannel.Dispose();
            }
            catch (Exception e)
            {
                _logger.Log($"Failed to close multicast publisher selector for: '{_name}'", e);
            }
            
            try
            {
                _readChannel.Close();
            }
            catch (Exception e)
            {
                _logger.Log($"Failed to close multicast reader channel for: '{_name}'", e);
            }
        }

        public async Task ProcessChannelAsync()
        {
            if (_closed)
            {
                return;
            }

            try
            {
                await SendMaxAsync();
            }
            catch (SocketException e)
            {
                _logger.Log($"Failed to read channel selector for: '{_name}'", e);
            }
        }

        public void SendAvailability() => Send(_availability);

        public void Send(RawMessage message)
        {
            var length = message.Length;

            if (length <= 0)
            {
                throw new ArgumentException("The message length must be greater than zero.");
            }

            if (length > _messageBuffer.Capacity)
            {
                throw new ArgumentException($"The message length is greater than {_messageBuffer.Capacity}");
            }
            
            _messageQueue.Enqueue(message);
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
        
        //====================================
        // ChannelMessageDispatcher
        //====================================

        public override IChannelReaderConsumer Consumer => _consumer;

        public override ILogger Logger => _logger;

        public override string Name => _name;
        
        //====================================
        // internal implementation
        //====================================
        
        private RawMessage AvailabilityMessage()
        {
            var message = new PublisherAvailability(
                _name,
                GetHostName(_publisherAddress.Address),
                _publisherAddress.Port).ToString();
            
            var buffer = new MemoryStream(message.Length);
            var messageBytes = Converters.TextToBytes(message);
            buffer.Write(messageBytes, 0, messageBytes.Length);
            buffer.Flip();

            return RawMessage.ReadFromWithoutHeader(buffer);
        }

        private string GetHostName(IPAddress publisherAddress)
        {
            try
            {
                return Dns.GetHostEntry(publisherAddress).HostName;
            }
            catch
            {
                return publisherAddress.ToString();
            }
        }

        private async Task SendMaxAsync()
        {
            while (true)
            {
                if (_messageQueue.Count == 0)
                {
                    return;
                }
                
                var message = _messageQueue.Peek();
                
                var sent = await _publisherChannel.SendToAsync(new ArraySegment<byte>(message.AsBuffer(_messageBuffer)),
                                    SocketFlags.None, _groupAddress);

                if (sent > 0)
                {
                    _messageQueue.Dequeue();
                }
                else
                {
                    return;
                }
            }
        }
    }
}
