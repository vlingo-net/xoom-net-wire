// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace Vlingo.Wire.Fdx.Outbound
{
    using Node;
    
    public class ManagedOutboundSocketChannelProvider : IManagedOutboundChannelProvider
    {
        private readonly IConfiguration _configuration;
        private readonly Node _node;
        private readonly Dictionary<Id, IManagedOutboundChannel> _nodeChannels;
        private readonly AddressType _type;

        public ManagedOutboundSocketChannelProvider(Node node, AddressType type, IConfiguration configuration)
        {
            _node = node;
            _type = type;
            _configuration = configuration;
            _nodeChannels = new Dictionary<Id, IManagedOutboundChannel>();

            ConfigureKnownChannels();
        }

        public IReadOnlyDictionary<Id, IManagedOutboundChannel> AllOtherNodeChannels =>
            ChannelsFor(_configuration.AllOtherNodes(_node.Id));
        
        public IManagedOutboundChannel ChannelFor(Id id)
        {
            if (_nodeChannels.TryGetValue(id, out var channel))
            {
                return channel;
            }

            var unopenedChannel = UnopenedChannelFor(_configuration.NodeMatching(id));
            _nodeChannels.Add(id, unopenedChannel);

            return unopenedChannel;
        }

        public IReadOnlyDictionary<Id, IManagedOutboundChannel> ChannelsFor(IEnumerable<Node> nodes)
        {
            var channels = new Dictionary<Id, IManagedOutboundChannel>();

            foreach (var node in nodes)
            {
                if (!_nodeChannels.TryGetValue(node.Id, out var channel))
                {
                    channel = UnopenedChannelFor(node);
                    _nodeChannels.Add(node.Id, channel);
                }
                
                channels.Add(node.Id, channel);
            }

            return channels;
        }

        public void Close()
        {
            foreach (var channel in _nodeChannels.Values)
            {
                channel.Close();
            }
            
            _nodeChannels.Clear();
        }

        public void Close(Id id)
        {
            if (_nodeChannels.TryGetValue(id, out var channel))
            {
                channel.Close();
                _nodeChannels.Remove(id);
            }
        }
        
        private void ConfigureKnownChannels()
        {
            foreach (var node in _configuration.AllOtherNodes(_node.Id))
            {
                _nodeChannels.Add(node.Id, UnopenedChannelFor(node));
            }
        }

        private IManagedOutboundChannel UnopenedChannelFor(Node node)
        {
            var address = _type.IsOperational ? _node.OperationalAddress : _node.ApplicationAddress;

            return new ManagedOutboundSocketChannel(node, address, _configuration.Logger);
        }
    }
}