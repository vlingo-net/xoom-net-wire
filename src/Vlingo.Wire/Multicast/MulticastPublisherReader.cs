// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Multicast
{
    public class MulticastPublisherReader : ChannelMessageDispatcher, IChannelPublisher
    {
        private readonly RawMessage _availability;
        private readonly UdpClient _publisherChannel;
        private bool _closed;
        private readonly IChannelReaderConsumer _consumer;
        private readonly IPEndPoint _groupAddress;
        private readonly ILogger _logger;
        private readonly MemoryStream _messageBuffer;
        private readonly Queue<RawMessage> _messageQueue;
        private readonly string _name;
        private readonly IPEndPoint _publisherAddress;
        private readonly Socket _readChannel;

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void ProcessChannel()
        {
            throw new System.NotImplementedException();
        }

        public void SendAvailability()
        {
            throw new System.NotImplementedException();
        }

        public void Send(RawMessage message)
        {
            throw new System.NotImplementedException();
        }

        public override IChannelReaderConsumer Consumer { get; }

        public override ILogger Logger { get; }

        public override string Name { get; }
    }
}
