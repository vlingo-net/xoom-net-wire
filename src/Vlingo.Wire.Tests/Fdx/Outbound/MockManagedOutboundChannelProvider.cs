// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Wire.Fdx.Outbound;

namespace Vlingo.Wire.Tests.Fdx.Outbound
{
    using Vlingo.Wire.Node;
    
    public class MockManagedOutboundChannelProvider : IManagedOutboundChannelProvider
    {
        private readonly Dictionary<Id, IManagedOutboundChannel> _allChannels = new Dictionary<Id, IManagedOutboundChannel>();
        private readonly IConfiguration _configuration;
        private Id _localNodeId;

        public MockManagedOutboundChannelProvider(Id localNodeId, IConfiguration configuration)
        {
            _localNodeId = localNodeId;
            _configuration = configuration;

            foreach (var node in _configuration.AllNodes)
            {
                _allChannels.Add(node.Id, new MockManagedOutboundChannel(node.Id));
            }
        }

        public IReadOnlyDictionary<Id, IManagedOutboundChannel> AllOtherNodeChannels
        {
            get
            {
                var others = new Dictionary<Id, IManagedOutboundChannel>();

                foreach (var node in _configuration.AllNodes)
                {
                    if (!node.Id.Equals(_localNodeId))
                    {
                        others.Add(node.Id, _allChannels[node.Id]);
                    }
                }

                return others;
            }
        }

        public IManagedOutboundChannel ChannelFor(Id id)
        {
            return _allChannels[id];
        }

        public IReadOnlyDictionary<Id, IManagedOutboundChannel> ChannelsFor(IEnumerable<Node> nodes)
        {
            var others = new Dictionary<Id, IManagedOutboundChannel>();

            foreach (var node in nodes)
            {
                if (!node.Id.Equals(_localNodeId))
                {
                    others.Add(node.Id, _allChannels[node.Id]);
                }
            }

            return others;
        }

        public void Close()
        {
        }

        public void Close(Id id)
        {
        }
    }
}