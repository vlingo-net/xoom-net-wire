// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net.Sockets;
using Vlingo.Xoom.Actors;

namespace Vlingo.Xoom.Wire.Channel;

public class SocketChannelSelectionProcessor__Proxy : ISocketChannelSelectionProcessor
{
    private const string CloseRepresentation1 = "Close()";
    private const string ProcessRepresentation2 = "Process(Socket)";

    private readonly Actor _actor;
    private readonly IMailbox _mailbox;

    public SocketChannelSelectionProcessor__Proxy(Actor actor, IMailbox mailbox)
    {
        _actor = actor;
        _mailbox = mailbox;
    }

    public void Close()
    {
        if (!_actor.IsStopped)
        {
            Action<ISocketChannelSelectionProcessor> consumer = x => x.Close();
            if (_mailbox.IsPreallocated)
            {
                _mailbox.Send(_actor, consumer, null, CloseRepresentation1);
            }
            else
            {
                _mailbox.Send(
                    new LocalMessage<ISocketChannelSelectionProcessor>(_actor, consumer, CloseRepresentation1));
            }
        }
        else
        {
            _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, CloseRepresentation1));
        }
    }

    public void Process(Socket channel)
    {
        if (!_actor.IsStopped)
        {
            Action<ISocketChannelSelectionProcessor> consumer = x => x.Process(channel);
            if (_mailbox.IsPreallocated)
            {
                _mailbox.Send(_actor, consumer, null, ProcessRepresentation2);
            }
            else
            {
                _mailbox.Send(
                    new LocalMessage<ISocketChannelSelectionProcessor>(_actor, consumer,
                        ProcessRepresentation2));
            }
        }
        else
        {
            _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, ProcessRepresentation2));
        }
    }
}