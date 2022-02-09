// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Outbound
{
    public class ApplicationOutboundStreamActor : Actor, IApplicationOutboundStream
    {
        private readonly Outbound _outbound;

        public ApplicationOutboundStreamActor(IManagedOutboundChannelProvider provider, ConsumerByteBufferPool byteBufferPool)
        {
            _outbound = new Outbound(provider, byteBufferPool);
        }
        
        //===================================
        // ClusterApplicationOutboundStream
        //===================================

        public void Broadcast(RawMessage message) => _outbound.Broadcast(message);

        public void SendTo(RawMessage message, Id targetId) => _outbound.SendTo(message, targetId);
        
        //===================================
        // Stoppable
        //===================================

        public override void Stop()
        {
            _outbound.Close();
            base.Stop();
        }
    }
}