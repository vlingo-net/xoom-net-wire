// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Outbound
{
    public interface IApplicationOutboundStream : IStoppable
    {
        void Broadcast(RawMessage message);
        
        void SendTo(RawMessage message, Id targetId);
    }

    public static class ApplicationOutboundStreamFactory
    {
        public static IApplicationOutboundStream Instance(
            Stage stage,
            IManagedOutboundChannelProvider provider,
            ConsumerByteBufferPool byteBufferPool)
        {
            var applicationOutboundStream =
                stage.ActorFor<IApplicationOutboundStream>(
                    () => new ApplicationOutboundStreamActor(provider, byteBufferPool), "application-outbound-stream");

            return applicationOutboundStream;
        }
    }
}