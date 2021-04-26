// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;

namespace Vlingo.Xoom.Wire.Channel
{
    public class ResponseSenderChannel__Proxy : IResponseSenderChannel
    {
        private const string AbandonRepresentation1 = "Abandon(Vlingo.Xoom.Wire.Channel.RequestResponseContext)";

        private const string RespondWithRepresentation2 =
            "RespondWith(Vlingo.Xoom.Wire.Channel.RequestResponseContext, Vlingo.Xoom.Wire.Message.IConsumerByteBuffer)";

        private const string RespondWithRepresentation3 =
            "RespondWith(Vlingo.Xoom.Wire.Channel.RequestResponseContext, Vlingo.Xoom.Wire.Message.IConsumerByteBuffer, bool)";

        private const string RespondWithRepresentation4 =
            "RespondWith(Vlingo.Xoom.Wire.Channel.RequestResponseContext, object, bool)";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public ResponseSenderChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }

        public void Abandon(RequestResponseContext context)
        {
            if (!_actor.IsStopped)
            {
                Action<IResponseSenderChannel> cons8585439 = __ => __.Abandon(context);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons8585439, null, AbandonRepresentation1);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IResponseSenderChannel>(_actor, cons8585439,
                            AbandonRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, AbandonRepresentation1));
            }
        }

        public void RespondWith(RequestResponseContext context,
            Message.IConsumerByteBuffer buffer)
        {
            if (!_actor.IsStopped)
            {
                Action<IResponseSenderChannel>
                    cons851152435 = __ => __.RespondWith(context, buffer);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons851152435, null, RespondWithRepresentation2);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IResponseSenderChannel>(_actor,
                        cons851152435, RespondWithRepresentation2));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, RespondWithRepresentation2));
            }
        }

        public void RespondWith(RequestResponseContext context,
            Message.IConsumerByteBuffer buffer, bool closeFollowing)
        {
            if (!_actor.IsStopped)
            {
                Action<IResponseSenderChannel> cons829071244 = __ =>
                    __.RespondWith(context, buffer, closeFollowing);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons829071244, null, RespondWithRepresentation3);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IResponseSenderChannel>(_actor,
                        cons829071244, RespondWithRepresentation3));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, RespondWithRepresentation3));
            }
        }

        public void RespondWith(RequestResponseContext context, object response,
            bool closeFollowing)
        {
            if (!_actor.IsStopped)
            {
                Action<IResponseSenderChannel> cons1528567494 = __ =>
                    __.RespondWith(context, response, closeFollowing);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons1528567494, null, RespondWithRepresentation4);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IResponseSenderChannel>(_actor,
                        cons1528567494, RespondWithRepresentation4));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, RespondWithRepresentation4));
            }
        }
    }
}