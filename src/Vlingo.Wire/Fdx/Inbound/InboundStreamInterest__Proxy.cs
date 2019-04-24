using System;
using Vlingo.Actors;
using Vlingo.Wire.Node;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Inbound
{
    public class InboundStreamInterest__Proxy : IInboundStreamInterest
    {
        private const string HandleInboundStreamMessageRepresentation1 =
            "HandleInboundStreamMessage(AddressType, RawMessage)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public InboundStreamInterest__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void HandleInboundStreamMessage(AddressType addressType, RawMessage message)
        {
            if (!actor.IsStopped)
            {
                Action<IInboundStreamInterest> consumer = x => x.HandleInboundStreamMessage(addressType, message);
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, HandleInboundStreamMessageRepresentation1);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IInboundStreamInterest>(actor, consumer,
                        HandleInboundStreamMessageRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, HandleInboundStreamMessageRepresentation1));
            }
        }
    }
}