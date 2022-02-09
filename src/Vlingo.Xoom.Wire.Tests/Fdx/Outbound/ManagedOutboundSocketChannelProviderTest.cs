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

namespace Vlingo.Xoom.Wire.Tests.Fdx.Outbound
{
    public class ManagedOutboundSocketChannelProviderTest : AbstractMessageTool
    {
        private readonly IEnumerable<Node> _allOtherNodes;
        private readonly ManagedOutboundSocketChannelProvider _provider;

        [Fact]
        public void TestProviderProvides()
        {
            Assert.Equal(2, _provider.AllOtherNodeChannels.Count);
            Assert.NotNull(_provider.ChannelFor(Id.Of(2)));
            Assert.NotNull(_provider.ChannelFor(Id.Of(3)));
            Assert.Equal(2, _provider.ChannelsFor(_allOtherNodes).Count);
        }
        
        [Fact]
        public void TestProviderCloseAllReopen()
        {
            _provider.Close();
            
            Assert.NotNull(_provider.ChannelFor(Id.Of(3)));
            Assert.NotNull(_provider.ChannelFor(Id.Of(2)));
            Assert.NotNull(_provider.ChannelFor(Id.Of(1)));
            
            Assert.Equal(2, _provider.AllOtherNodeChannels.Count);
        }
        
        [Fact]
        public void TestProviderCloseOneChannelReopen()
        {
            _provider.Close(Id.Of(3));
            
            Assert.NotNull(_provider.ChannelFor(Id.Of(3)));
            
            Assert.Equal(2, _provider.AllOtherNodeChannels.Count);
        }

        public ManagedOutboundSocketChannelProviderTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            _allOtherNodes = Config.AllOtherNodes(Id.Of(1));
            _provider = new ManagedOutboundSocketChannelProvider(Config.NodeMatching(Id.Of(1)), AddressType.Op, Config);
        }
    }
}