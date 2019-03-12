// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Outbound
{
    public class ManagedOutboundSocketChannelProvider : IManagedOutboundChannelProvider
    {
        public IReadOnlyDictionary<Id, IManagedOutboundChannel> AllOtherNodeChannels { get; }
        
        public IManagedOutboundChannel ChannelFor(Id id)
        {
            throw new System.NotImplementedException();
        }

        public IReadOnlyDictionary<Id, IManagedOutboundChannel> ChannelsFor(IEnumerable<Node.Node> nodes)
        {
            throw new System.NotImplementedException();
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void Close(Id id)
        {
            throw new System.NotImplementedException();
        }
    }
}