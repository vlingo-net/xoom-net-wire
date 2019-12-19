using System;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class ServerRequestResponseChannel__Proxy : IServerRequestResponseChannel
    {
        private const string RepresentationConclude0 = "Conclude()";
        private const string StartRepresentation1 = "Start()";
        private const string StopRepresentation2 = "Stop()";
        private const string CloseRepresentation3 = "Close()";
        private const string RepresentationPort4 = "Port()";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ServerRequestResponseChannel__Proxy(Actor actor, IMailbox mailbox)
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

        public ICompletes<int> Port()
        {
            if (!actor.IsStopped)
            {
                var completes = Completes.Using<int>(actor.Scheduler);
                Action<IServerRequestResponseChannel> consumer = x => x.Port();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, completes, RepresentationPort4);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IServerRequestResponseChannel>(actor, consumer, completes, RepresentationPort4));
                }
                return completes;
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, RepresentationPort4));
            }

            return null!;
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