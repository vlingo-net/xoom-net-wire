// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Common;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class ServerRequestResponseChannel__Proxy : IServerRequestResponseChannel
    {
        private const string StartRepresentation1 =
            "Start(Vlingo.Xoom.Actors.Stage, Vlingo.Xoom.Wire.Channel.IRequestChannelConsumerProvider, int, string, int, int, int, long, long)";

        private const string StartRepresentation2 =
            "Start(Vlingo.Xoom.Actors.Stage, Vlingo.Xoom.Actors.IAddress, string, Vlingo.Xoom.Wire.Channel.IRequestChannelConsumerProvider, int, string, int, int, int, long, long)";

        private const string CloseRepresentation3 = "Close()";
        private const string PortRepresentation4 = "Port()";
        private const string ConcludeRepresentation5 = "Conclude()";
        private const string StopRepresentation6 = "Stop()";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public ServerRequestResponseChannel__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }

        public bool IsStopped => false;

        public IServerRequestResponseChannel Start(Stage stage,
            Channel.IRequestChannelConsumerProvider provider, int port, string name, int processorPoolSize,
            int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout)
        {
            if (!_actor.IsStopped)
            {
                Action<IServerRequestResponseChannel> cons843064929 = __ =>
                    __.Start(stage, provider, port, name, processorPoolSize, maxBufferPoolSize, maxMessageSize,
                        probeInterval, probeTimeout);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons843064929, null, StartRepresentation1);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IServerRequestResponseChannel>(_actor,
                            cons843064929, StartRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StartRepresentation1));
            }

            return null!;
        }

        public IServerRequestResponseChannel Start(Stage stage,
            IAddress address, string mailboxName,
            Channel.IRequestChannelConsumerProvider provider, int port, string name, int processorPoolSize,
            int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout)
        {
            if (!_actor.IsStopped)
            {
                Action<IServerRequestResponseChannel> cons1871902676 = __ =>
                    __.Start(stage, address, mailboxName, provider, port, name, processorPoolSize, maxBufferPoolSize,
                        maxMessageSize, probeInterval, probeTimeout);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons1871902676, null, StartRepresentation2);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IServerRequestResponseChannel>(_actor,
                            cons1871902676, StartRepresentation2));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StartRepresentation2));
            }

            return null!;
        }

        public void Close()
        {
            if (!_actor.IsStopped)
            {
                Action<IServerRequestResponseChannel> cons1365492750 = __ => __.Close();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons1365492750, null, CloseRepresentation3);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IServerRequestResponseChannel>(_actor,
                            cons1365492750, CloseRepresentation3));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, CloseRepresentation3));
            }
        }

        public ICompletes<int> Port()
        {
            if (!_actor.IsStopped)
            {
                Action<IServerRequestResponseChannel> cons1657589136 = __ => __.Port();
                var completes = new BasicCompletes<int>(_actor.Scheduler);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons1657589136, completes, PortRepresentation4);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IServerRequestResponseChannel>(_actor,
                            cons1657589136, completes, PortRepresentation4));
                }

                return completes;
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, PortRepresentation4));
            }

            return null!;
        }

        public void Conclude()
        {
            if (!_actor.IsStopped)
            {
                Action<IServerRequestResponseChannel> cons347762824 = __ => __.Conclude();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons347762824, null, ConcludeRepresentation5);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IServerRequestResponseChannel>(_actor,
                            cons347762824, ConcludeRepresentation5));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, ConcludeRepresentation5));
            }
        }

        public void Stop()
        {
            if (!_actor.IsStopped)
            {
                Action<IServerRequestResponseChannel> cons1070440153 = __ => __.Stop();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, cons1070440153, null, StopRepresentation6);
                }
                else
                {
                    _mailbox.Send(
                        new LocalMessage<IServerRequestResponseChannel>(_actor,
                            cons1070440153, StopRepresentation6));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StopRepresentation6));
            }
        }
    }
}