using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Common;

namespace Vlingo.Wire.Channel
{
    public class ResponseChannelConsumer__Proxy : Vlingo.Wire.Channel.IResponseChannelConsumer
    {
        private const string ConsumeRepresentation1 = "Consume(Vlingo.Wire.Message.IConsumerByteBuffer)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ResponseChannelConsumer__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void Consume(Vlingo.Wire.Message.IConsumerByteBuffer buffer)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Channel.IResponseChannelConsumer> cons128873 = __ => __.Consume(buffer);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons128873, null, ConsumeRepresentation1);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Channel.IResponseChannelConsumer>(this.actor, cons128873,
                            ConsumeRepresentation1));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, ConsumeRepresentation1));
            }
        }
    }
}