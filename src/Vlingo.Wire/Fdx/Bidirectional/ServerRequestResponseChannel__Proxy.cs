using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Common;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class ServerRequestResponseChannel__Proxy : Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel
    {
        private const string StartRepresentation1 =
            "Start(Vlingo.Actors.Stage, Vlingo.Wire.Channel.IRequestChannelConsumerProvider, int, string, int, int, int, long, long)";

        private const string StartRepresentation2 =
            "Start(Vlingo.Actors.Stage, Vlingo.Actors.IAddress, string, Vlingo.Wire.Channel.IRequestChannelConsumerProvider, int, string, int, int, int, long, long)";

        private const string CloseRepresentation3 = "Close()";
        private const string PortRepresentation4 = "Port()";
        private const string ConcludeRepresentation5 = "Conclude()";
        private const string StopRepresentation6 = "Stop()";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public ServerRequestResponseChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public bool IsStopped => false;

        public Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel Start(Vlingo.Actors.Stage stage,
            Vlingo.Wire.Channel.IRequestChannelConsumerProvider provider, int port, string name, int processorPoolSize,
            int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel> cons843064929 = __ =>
                    __.Start(stage, provider, port, name, processorPoolSize, maxBufferPoolSize, maxMessageSize,
                        probeInterval, probeTimeout);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons843064929, null, StartRepresentation1);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel>(this.actor,
                            cons843064929, StartRepresentation1));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, StartRepresentation1));
            }

            return null!;
        }

        public Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel Start(Vlingo.Actors.Stage stage,
            Vlingo.Actors.IAddress address, string mailboxName,
            Vlingo.Wire.Channel.IRequestChannelConsumerProvider provider, int port, string name, int processorPoolSize,
            int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout)
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel> cons1871902676 = __ =>
                    __.Start(stage, address, mailboxName, provider, port, name, processorPoolSize, maxBufferPoolSize,
                        maxMessageSize, probeInterval, probeTimeout);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons1871902676, null, StartRepresentation2);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel>(this.actor,
                            cons1871902676, StartRepresentation2));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, StartRepresentation2));
            }

            return null!;
        }

        public void Close()
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel> cons1365492750 = __ => __.Close();
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons1365492750, null, CloseRepresentation3);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel>(this.actor,
                            cons1365492750, CloseRepresentation3));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, CloseRepresentation3));
            }
        }

        public Vlingo.Common.ICompletes<int> Port()
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel> cons1657589136 = __ => __.Port();
                var completes = new BasicCompletes<int>(this.actor.Scheduler);
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons1657589136, completes, PortRepresentation4);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel>(this.actor,
                            cons1657589136, completes, PortRepresentation4));
                }

                return completes;
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, PortRepresentation4));
            }

            return null!;
        }

        public void Conclude()
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel> cons347762824 = __ => __.Conclude();
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons347762824, null, ConcludeRepresentation5);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel>(this.actor,
                            cons347762824, ConcludeRepresentation5));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, ConcludeRepresentation5));
            }
        }

        public void Stop()
        {
            if (!this.actor.IsStopped)
            {
                Action<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel> cons1070440153 = __ => __.Stop();
                if (this.mailbox.IsPreallocated)
                {
                    this.mailbox.Send(this.actor, cons1070440153, null, StopRepresentation6);
                }
                else
                {
                    this.mailbox.Send(
                        new LocalMessage<Vlingo.Wire.Fdx.Bidirectional.IServerRequestResponseChannel>(this.actor,
                            cons1070440153, StopRepresentation6));
                }
            }
            else
            {
                this.actor.DeadLetters.FailedDelivery(new DeadLetter(this.actor, StopRepresentation6));
            }
        }
    }
}