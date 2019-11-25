// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Actors;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Outbound
{
    public class ApplicationOutboundStream__Proxy : IApplicationOutboundStream
    {
        private const string RepresentationConclude0 = "Conclude()";
        private const string RepresentationStop1 = "Stop()";
        private const string RepresentationBroadcast2 = "Broadcast(RawMessage)";
        private const string RepresentationSendTo3 = "SendTo(RawMessage, Id)";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public ApplicationOutboundStream__Proxy(Actor actor, IMailbox mailbox)
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
                _actor.DeadLetters.FailedDelivery(new DeadLetter(_actor, RepresentationConclude0));
            }
        }
        
        public void Stop()
        {
            if (!_actor.IsStopped)
            {
                Action<IApplicationOutboundStream> consumer = actor => actor.Stop();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, RepresentationStop1);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IApplicationOutboundStream>(_actor, consumer, RepresentationStop1));
                }
            }
            else
            {
                _actor.DeadLetters.FailedDelivery(new DeadLetter(_actor, RepresentationStop1));
            }
        }

        public bool IsStopped => _actor.IsStopped;
        
        public void Broadcast(RawMessage message)
        {
            if (!_actor.IsStopped)
            {
                Action<IApplicationOutboundStream> consumer = actor => actor.Broadcast(message);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, RepresentationBroadcast2);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IApplicationOutboundStream>(_actor, consumer, RepresentationBroadcast2));
                }
            }
            else
            {
                _actor.DeadLetters.FailedDelivery(new DeadLetter(_actor, RepresentationBroadcast2));
            }
        }

        public void SendTo(RawMessage message, Id targetId)
        {
            if (!_actor.IsStopped)
            {
                Action<IApplicationOutboundStream> consumer = actor => actor.SendTo(message, targetId);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, RepresentationSendTo3);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IApplicationOutboundStream>(_actor, consumer, RepresentationSendTo3));
                }
            }
            else
            {
                _actor.DeadLetters.FailedDelivery(new DeadLetter(_actor, RepresentationSendTo3));
            }
        }
    }
}