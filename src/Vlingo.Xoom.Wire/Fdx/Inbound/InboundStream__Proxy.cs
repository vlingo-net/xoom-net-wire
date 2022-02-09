// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;

namespace Vlingo.Xoom.Wire.Fdx.Inbound
{
    public class InboundStream__Proxy : IInboundStream
    {
        private const string RepresentationConclude0 = "Conclude()";
        private const string StartRepresentation1 = "Start()";
        private const string StopRepresentation2 = "Stop()";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public InboundStream__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }
        
        public void Conclude()
        {
            if (!_actor.IsStopped)
            {
                Action<IStoppable> consumer = x => x.Conclude();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, RepresentationConclude0);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IStoppable>(_actor, consumer, RepresentationConclude0));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, RepresentationConclude0));
            }
        }

        public bool IsStopped => false;

        public void Start()
        {
            if (!_actor.IsStopped)
            {
                Action<IInboundStream> consumer = x => x.Start();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, StartRepresentation1);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IInboundStream>(_actor, consumer, StartRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StartRepresentation1));
            }
        }

        public void Stop()
        {
            if (!_actor.IsStopped)
            {
                Action<IInboundStream> consumer = x => x.Stop();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, StopRepresentation2);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IInboundStream>(_actor, consumer, StopRepresentation2));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StopRepresentation2));
            }
        }
    }
}