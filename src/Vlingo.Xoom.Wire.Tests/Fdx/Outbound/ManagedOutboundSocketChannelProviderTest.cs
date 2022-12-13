// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Vlingo.Xoom.Wire.Fdx.Outbound;
using Vlingo.Xoom.Wire.Nodes;
using Vlingo.Xoom.Wire.Tests.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Xoom.Wire.Tests.Fdx.Outbound;

public class ManagedOutboundSocketChannelProviderTest : AbstractMessageTool
{
    private readonly ManagedOutboundSocketChannelProvider _provider;

    [Fact]
    public void TestProviderProvides()
    {
        Assert.Equal(0, _provider.AllOtherNodeChannels.Count); // channels are lazily created

        var nodeIds = new List<Node>();
        nodeIds.Add(Config.NodeMatching(Id.Of(2)));
        nodeIds.Add(Config.NodeMatching(Id.Of(3)));

        Assert.NotNull(_provider.ChannelFor(nodeIds[0]));
        Assert.NotNull(_provider.ChannelFor(nodeIds[1]));

        Assert.Equal(2, _provider.ChannelsFor(nodeIds).Count);
    }
        
    [Fact]
    public void TestProviderCloseAllReopen()
    {
        _provider.Close();
            
        Assert.NotNull(_provider.ChannelFor(Config.NodeMatching(Id.Of(3))));
        Assert.NotNull(_provider.ChannelFor(Config.NodeMatching(Id.Of(2))));
        Assert.NotNull(_provider.ChannelFor(Config.NodeMatching(Id.Of(1))));
            
        Assert.Equal(2, _provider.AllOtherNodeChannels.Count);
    }
        
    [Fact]
    public void TestProviderCloseOneChannelReopen()
    {
        var nodeIds = new List<Node>();
        nodeIds.Add(Config.NodeMatching(Id.Of(3)));
        nodeIds.Add(Config.NodeMatching(Id.Of(2)));

        Assert.NotNull(_provider.ChannelFor(nodeIds[0])); // channels are created on demand; create the channel
        _provider.Close(nodeIds[0].Id);

        Assert.NotNull(_provider.ChannelFor(nodeIds[0]));
        Assert.Equal(1, _provider.AllOtherNodeChannels.Count);

        Assert.NotNull(_provider.ChannelFor(nodeIds[1])); // create the channel
        Assert.Equal(2, _provider.AllOtherNodeChannels.Count);
    }

    public ManagedOutboundSocketChannelProviderTest(ITestOutputHelper output)
    {
        var converter = new Converter(output);
        Console.SetOut(converter);
        _provider = new ManagedOutboundSocketChannelProvider(Config.NodeMatching(Id.Of(1)), AddressType.Op, TestWorld.DefaultLogger);
    }
}