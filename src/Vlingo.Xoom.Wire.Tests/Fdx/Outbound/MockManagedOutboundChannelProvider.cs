// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Xoom.Wire.Fdx.Outbound;
using Vlingo.Xoom.Wire.Nodes;
using Vlingo.Xoom.Wire.Tests.Nodes;

namespace Vlingo.Xoom.Wire.Tests.Fdx.Outbound;

public class MockManagedOutboundChannelProvider : IManagedOutboundChannelProvider
{
    private readonly Dictionary<Id, IManagedOutboundChannel> _allChannels = new();
    private readonly MockConfiguration _configuration;
    private readonly Id _localNodeId;

    public MockManagedOutboundChannelProvider(Id localNodeId, MockConfiguration configuration)
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

    public IManagedOutboundChannel ChannelFor(Node node) => _allChannels[node.Id];
    
    public IManagedOutboundChannel ChannelFor(Id id) => _allChannels[id];

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