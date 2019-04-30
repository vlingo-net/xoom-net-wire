using System;
using Vlingo.Actors;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class ChannelReaderConsumer__Proxy : IChannelReaderConsumer
    {
        private const string ConsumeRepresentation1 = "Consume(RawMessage)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ChannelReaderConsumer__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void Consume(RawMessage message)
        {
            if (!actor.IsStopped)
            {
                Action<IChannelReaderConsumer> consumer = x => x.Consume(message);
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, ConsumeRepresentation1);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IChannelReaderConsumer>(actor, consumer, ConsumeRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, ConsumeRepresentation1));
            }
        }
    }
}