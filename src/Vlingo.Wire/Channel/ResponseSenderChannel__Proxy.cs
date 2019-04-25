// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Actors;
using System.Net.Sockets;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class ResponseSenderChannel__Proxy : IResponseSenderChannel<Socket>
    {
        private const string AbandonRepresentation1 = "Abandon(RequestResponseContext<Socket>)";
        private const string RespondWithRepresentation2 = "RespondWith(RequestResponseContext<Socket>, IConsumerByteBuffer)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ResponseSenderChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void Abandon(RequestResponseContext<Socket> context)
        {
            if(!actor.IsStopped)
            {
                Action<IResponseSenderChannel<Socket>> consumer = x => x.Abandon(context);
                if(mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, AbandonRepresentation1);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IResponseSenderChannel<Socket>>(actor, consumer, AbandonRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, AbandonRepresentation1));
            }
        }
        public void RespondWith(RequestResponseContext<Socket> context, IConsumerByteBuffer buffer)
        {
            if(!actor.IsStopped)
            {
                Action<IResponseSenderChannel<Socket>> consumer = x => x.RespondWith(context, buffer);
                if(mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, RespondWithRepresentation2);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IResponseSenderChannel<Socket>>(actor, consumer, RespondWithRepresentation2));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, RespondWithRepresentation2));
            }
        }

    }
}