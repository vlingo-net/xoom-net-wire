// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Actors;

namespace Vlingo.Wire.Channel
{
    public class ResponseChannelConsumer__Proxy : IResponseChannelConsumer
    {
        private const string ConsumeRepresentation1 = "Consume(Vlingo.Wire.Message.IConsumerByteBuffer)";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public ResponseChannelConsumer__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }

        public void Consume(Message.IConsumerByteBuffer buffer)
        {
            if (!_actor.IsStopped)
            {
                Action<IResponseChannelConsumer> cons128873 = __ => __.Consume(buffer);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons128873, null, ConsumeRepresentation1);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IResponseChannelConsumer>(_actor, cons128873,
                            ConsumeRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, ConsumeRepresentation1));
            }
        }
    }
}