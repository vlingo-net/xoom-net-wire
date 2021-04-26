// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Inbound
{
    public class InboundStreamInterest__Proxy : IInboundStreamInterest
    {
        private const string HandleInboundStreamMessageRepresentation1 =
            "HandleInboundStreamMessage(AddressType, RawMessage)";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public InboundStreamInterest__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }

        public void HandleInboundStreamMessage(AddressType addressType, RawMessage message)
        {
            if (!_actor.IsStopped)
            {
                Action<IInboundStreamInterest> consumer = x => x.HandleInboundStreamMessage(addressType, message);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, HandleInboundStreamMessageRepresentation1);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IInboundStreamInterest>(_actor, consumer,
                        HandleInboundStreamMessageRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, HandleInboundStreamMessageRepresentation1));
            }
        }
    }
}