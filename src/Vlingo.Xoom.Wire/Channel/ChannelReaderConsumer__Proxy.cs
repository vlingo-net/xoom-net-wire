// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Message;

namespace Vlingo.Xoom.Wire.Channel;

public class ChannelReaderConsumer__Proxy : IChannelReaderConsumer
{
    private const string ConsumeRepresentation1 = "Consume(RawMessage)";

    private readonly Actor _actor;
    private readonly IMailbox _mailbox;

    public ChannelReaderConsumer__Proxy(Actor actor, IMailbox mailbox)
    {
        _actor = actor;
        _mailbox = mailbox;
    }

    public void Consume(RawMessage message)
    {
        if (!_actor.IsStopped)
        {
            Action<IChannelReaderConsumer> consumer = x => x.Consume(message);
            if (_mailbox.IsPreallocated)
            {
                _mailbox.Send(_actor, consumer, null, ConsumeRepresentation1);
            }
            else
            {
                _mailbox.Send(new LocalMessage<IChannelReaderConsumer>(_actor, consumer, ConsumeRepresentation1));
            }
        }
        else
        {
            _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, ConsumeRepresentation1));
        }
    }
}