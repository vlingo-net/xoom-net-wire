using System;
using System.Net.Sockets;
using Vlingo.Actors;

namespace Vlingo.Wire.Channel
{
    public class SocketChannelSelectionProcessor__Proxy : ISocketChannelSelectionProcessor
    {
        private const string CloseRepresentation1 = "Close()";
        private const string ProcessRepresentation2 = "Process(Socket)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public SocketChannelSelectionProcessor__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public void Close()
        {
            if (!actor.IsStopped)
            {
                Action<ISocketChannelSelectionProcessor> consumer = x => x.Close();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, CloseRepresentation1);
                }
                else
                {
                    mailbox.Send(
                        new LocalMessage<ISocketChannelSelectionProcessor>(actor, consumer, CloseRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, CloseRepresentation1));
            }
        }

        public void Process(Socket channel)
        {
            if (!actor.IsStopped)
            {
                Action<ISocketChannelSelectionProcessor> consumer = x => x.Process(channel);
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, ProcessRepresentation2);
                }
                else
                {
                    mailbox.Send(
                        new LocalMessage<ISocketChannelSelectionProcessor>(actor, consumer,
                            ProcessRepresentation2));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, ProcessRepresentation2));
            }
        }
    }
}