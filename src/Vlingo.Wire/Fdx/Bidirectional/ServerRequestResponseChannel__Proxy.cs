using System;
using Vlingo.Actors;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class ServerRequestResponseChannel__Proxy : IServerRequestResponseChannel
    {
        private const string StartRepresentation1 = "Start()";
        private const string StopRepresentation2 = "Stop()";
        private const string CloseRepresentation3 = "Close()";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ServerRequestResponseChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public bool IsStopped => actor.IsStopped;

        public void Start()
        {
            if (!actor.IsStopped)
            {
                Action<ServerRequestResponseChannelActor> consumer = x => x.Start();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StartRepresentation1);
                }
                else
                {
                    mailbox.Send(
                        new LocalMessage<ServerRequestResponseChannelActor>(actor, consumer, StartRepresentation1));
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
                Action<ServerRequestResponseChannelActor> consumer = x => x.Stop();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StopRepresentation2);
                }
                else
                {
                    mailbox.Send(
                        new LocalMessage<ServerRequestResponseChannelActor>(actor, consumer, StopRepresentation2));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StopRepresentation2));
            }
        }

        public void Close()
        {
            if (!actor.IsStopped)
            {
                Action<ServerRequestResponseChannelActor> consumer = x => x.Close();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, CloseRepresentation3);
                }
                else
                {
                    mailbox.Send(
                        new LocalMessage<ServerRequestResponseChannelActor>(actor, consumer, CloseRepresentation3));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StopRepresentation2));
            }
        }

        public IServerRequestResponseChannel Start(Stage stage, IRequestChannelConsumerProvider provider, int port,
            string name, int processorPoolSize, int maxBufferPoolSize, int maxMessageSize, long probeInterval)
        {
            return ServerRequestResponseChannelFactory.Start(
                stage,
                provider,
                port,
                name,
                processorPoolSize,
                maxMessageSize,
                maxMessageSize,
                probeInterval);
        }

        public IServerRequestResponseChannel Start(Stage stage, IAddress address, string mailboxName,
            IRequestChannelConsumerProvider provider, int port, string name, int processorPoolSize,
            int maxBufferPoolSize, int maxMessageSize, long probeInterval)
        {
            return ServerRequestResponseChannelFactory.Start(
                stage,
                address,
                mailboxName,
                provider,
                port,
                name,
                processorPoolSize,
                maxMessageSize,
                maxMessageSize,
                probeInterval);
        }
    }
}