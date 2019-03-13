using System;
using System.Collections.Generic;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Wire.Fdx.Inbound;

namespace Vlingo.Wire.Fdx.Inbound
{
    public class InboundStream__Proxy : IInboundStream
    {
        private const string StartRepresentation1 = "Start()";
        private const string StopRepresentation2 = "Stop()";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public InboundStream__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public bool IsStopped => false;

        public void Start()
        {
            if (!actor.IsStopped)
            {
                Action<IInboundStream> consumer = x => x.Start();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StartRepresentation1);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IInboundStream>(actor, consumer, StartRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StartRepresentation1));
            }
        }

        public void Stop()
        {
            if (!actor.IsStopped)
            {
                Action<IInboundStream> consumer = x => x.Stop();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StopRepresentation2);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IInboundStream>(actor, consumer, StopRepresentation2));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StopRepresentation2));
            }
        }
    }
}