using System;
using System.Collections.Generic;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class ResponseSenderChannel__Proxy : IResponseSenderChannel
    {
        private const string AbandonRepresentation1 = "Abandon(RequestResponseContext<T>)";
        private const string RespondWithRepresentation2 = "RespondWith(RequestResponseContext<T>, IConsumerByteBuffer)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ResponseSenderChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void Abandon(RequestResponseContext context)
        {
            if (!this.actor.IsStopped)
            {
                Action<IResponseSenderChannel> consumer = __ => __.Abandon(context);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, consumer, null, AbandonRepresentation1);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<IResponseSenderChannel>(this.actor, consumer, AbandonRepresentation1));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, AbandonRepresentation1));
            }
        }

        public void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer)
        {
            if (!this.actor.IsStopped)
            {
                Action<IResponseSenderChannel> consumer = __ => __.RespondWith(context, buffer);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, consumer, null, RespondWithRepresentation2);
                }
                else
                {
                    this.mailbox.Send(new LocalMessage<IResponseSenderChannel>(this.actor, consumer,
                        RespondWithRepresentation2));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, RespondWithRepresentation2));
            }
        }
    }
}