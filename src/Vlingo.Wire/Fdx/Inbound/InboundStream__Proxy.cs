using System;
using Vlingo.Actors;

namespace Vlingo.Wire.Fdx.Inbound
{
    public class InboundStream__Proxy : IInboundStream
    {
        private const string RepresentationConclude0 = "Conclude()";
        private const string StartRepresentation1 = "Start()";
        private const string StopRepresentation2 = "Stop()";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public InboundStream__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }
        
        public void Conclude()
        {
            if (!actor.IsStopped)
            {
                Action<IStoppable> consumer = x => x.Conclude();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, RepresentationConclude0);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IStoppable>(actor, consumer, RepresentationConclude0));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, RepresentationConclude0));
            }
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