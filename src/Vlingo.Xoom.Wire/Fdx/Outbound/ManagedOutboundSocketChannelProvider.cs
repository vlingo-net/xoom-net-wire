// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Outbound;

public class ManagedOutboundSocketChannelProvider : IManagedOutboundChannelProvider
{
    private readonly IConfiguration? _configuration;
    private readonly ILogger? _logger;
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
    
    public ManagedOutboundSocketChannelProvider(Node node, AddressType type, ILogger logger)
    {
        _node = node;
        _type = type;
        _logger = logger;
        _nodeChannels = new Dictionary<Id, IManagedOutboundChannel>();
    }

    public IReadOnlyDictionary<Id, IManagedOutboundChannel> AllOtherNodeChannels
    {
        get
        {
            if (_configuration != null)
            {
                return ChannelsFor(_configuration.AllOtherNodes(_node.Id));
            }
            
            return _nodeChannels
                .Where(entry => !entry.Key.Equals(_node.Id))
                .ToDictionary(k => k.Key, v => v.Value);
        }
    }
    
    public IManagedOutboundChannel ChannelFor(Id id)
    {
        if (_nodeChannels.TryGetValue(id, out var channel))
        {
            return channel;
        }

        var unopenedChannel = UnopenedChannelFor(_configuration?.NodeMatching(id)!);
        _nodeChannels.Add(id, unopenedChannel);

        return unopenedChannel;
    }

    public IManagedOutboundChannel ChannelFor(Node node)
    {
        if (_nodeChannels.TryGetValue(node.Id, out var channel))
        {
            return channel;
        }

        var nodeAddress = AddressOf(node, _type);
        var unopenedChannel = UnopenedChannelFor(node, nodeAddress, _logger!);
        _nodeChannels.Add(node.Id, unopenedChannel);

        return unopenedChannel;
    }

    public IReadOnlyDictionary<Id, IManagedOutboundChannel> ChannelsFor(IEnumerable<Node> nodes)
    {
        var channels = new Dictionary<Id, IManagedOutboundChannel>();

        foreach (var node in nodes)
        {
            if (!_nodeChannels.TryGetValue(node.Id, out var channel))
            {
                if (_configuration != null)
                {
                    channel = UnopenedChannelFor(node);
                }
                else
                {
                    var nodeAddress = AddressOf(node, _type);
                    channel = UnopenedChannelFor(node, nodeAddress, _logger!);
                }
                
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
    
    protected Address AddressOf(Node node, AddressType type)
    {
        _logger?.Debug("addressOf {}", node);
        return Equals(type, AddressType.Op) ? node.OperationalAddress : node.ApplicationAddress;
    }
    
    private void ConfigureKnownChannels()
    {
        foreach (var node in _configuration?.AllOtherNodes(_node.Id)!)
        {
            _nodeChannels.Add(node.Id, UnopenedChannelFor(node));
        }
    }

    private IManagedOutboundChannel UnopenedChannelFor(Node node)
    {
        var address = _type.IsOperational ? _node.OperationalAddress : _node.ApplicationAddress;
        return new ManagedOutboundSocketChannel(node, address, _configuration?.Logger!);
    }

    private IManagedOutboundChannel UnopenedChannelFor(Node node, Address nodeAddress, ILogger logger) => 
        new ManagedOutboundSocketChannel(node, nodeAddress, logger);
}