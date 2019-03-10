// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Inbound
{
    public interface IInboundStream: IStartable, IStoppable
    {
    }

    public static class InboundStreamFactory
    {
        public static IInboundStream Instance(
            Stage stage,
            IInboundStreamInterest interest,
            int port,
            AddressType addressType,
            string inboundName,
            int maxMessageSize,
            long probeInterval)
        {
            var reader = new SocketChannelInboundReader(port, inboundName, maxMessageSize, stage.World.DefaultLogger);
            
            var definition =
                Definition.Has<InboundStreamActor>(Definition.Parameters(interest, addressType, reader, probeInterval),$"{inboundName}-inbound");

            var inboundStream = stage.ActorFor<IInboundStream>(definition);

            return inboundStream;
        }
    }
}