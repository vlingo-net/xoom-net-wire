using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Common;

namespace Vlingo.Wire.Channel
{
    public class ResponseSenderChannel__Proxy : Vlingo.Wire.Channel.IResponseSenderChannel
    {
        private const string AbandonRepresentation1 = "Abandon(Vlingo.Wire.Channel.RequestResponseContext)";

        private const string RespondWithRepresentation2 =
            "RespondWith(Vlingo.Wire.Channel.RequestResponseContext, Vlingo.Wire.Message.IConsumerByteBuffer)";

        private const string RespondWithRepresentation3 =
            "RespondWith(Vlingo.Wire.Channel.RequestResponseContext, Vlingo.Wire.Message.IConsumerByteBuffer, bool)";

        private const string RespondWithRepresentation4 =
            "RespondWith(Vlingo.Wire.Channel.RequestResponseContext, object, bool)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ResponseSenderChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void Abandon(Vlingo.Wire.Channel.RequestResponseContext context)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Channel.IResponseSenderChannel> cons8585439 = __ => __.Abandon(context);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons8585439, null, AbandonRepresentation1);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Channel.IResponseSenderChannel>(this.actor, cons8585439,
                            AbandonRepresentation1));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, AbandonRepresentation1));
            }
        }

        public void RespondWith(Vlingo.Wire.Channel.RequestResponseContext context,
            Vlingo.Wire.Message.IConsumerByteBuffer buffer)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Channel.IResponseSenderChannel>
                    cons851152435 = __ => __.RespondWith(context, buffer);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons851152435, null, RespondWithRepresentation2);
                }
                else
                {
                    this.mailbox.Send(new LocalMessage<Vlingo.Wire.Channel.IResponseSenderChannel>(this.actor,
                        cons851152435, RespondWithRepresentation2));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, RespondWithRepresentation2));
            }
        }

        public void RespondWith(Vlingo.Wire.Channel.RequestResponseContext context,
            Vlingo.Wire.Message.IConsumerByteBuffer buffer, bool closeFollowing)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Channel.IResponseSenderChannel> cons829071244 = __ =>
                    __.RespondWith(context, buffer, closeFollowing);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons829071244, null, RespondWithRepresentation3);
                }
                else
                {
                    this.mailbox.Send(new LocalMessage<Vlingo.Wire.Channel.IResponseSenderChannel>(this.actor,
                        cons829071244, RespondWithRepresentation3));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, RespondWithRepresentation3));
            }
        }

        public void RespondWith(Vlingo.Wire.Channel.RequestResponseContext context, object response,
            bool closeFollowing)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Channel.IResponseSenderChannel> cons1528567494 = __ =>
                    __.RespondWith(context, response, closeFollowing);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons1528567494, null, RespondWithRepresentation4);
                }
                else
                {
                    this.mailbox.Send(new LocalMessage<Vlingo.Wire.Channel.IResponseSenderChannel>(this.actor,
                        cons1528567494, RespondWithRepresentation4));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, RespondWithRepresentation4));
            }
        }
    }
}