// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public interface IServerRequestResponseChannel : IStoppable
    {
        IServerRequestResponseChannel Start(Stage stage,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout);

        IServerRequestResponseChannel Start(
            Stage stage,
            IAddress address,
            string mailboxName,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout);

        void Close();

        ICompletes<int> Port();
    }

    // TODO: This is an workaround because C# doesn't allow implementation of default methods in interfaces. Should be fixed with C# 8
    public static class ServerRequestResponseChannelFactory
    {
        public static IServerRequestResponseChannel Start(
            Stage stage,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout)
        {
            var parameters = Definition.Parameters(provider, port, name, processorPoolSize, maxBufferPoolSize, maxMessageSize, probeInterval, probeTimeout);

            var channel =
                stage.ActorFor<IServerRequestResponseChannel>(
                    Definition.Has<ServerRequestResponseChannelActor>(parameters));

            return channel;
        }
        
        public static IServerRequestResponseChannel Start(
            Stage stage,
            IAddress address,
            string mailboxName,
            IRequestChannelConsumerProvider provider,
            int port,
            string name,
            int processorPoolSize,
            int maxBufferPoolSize,
            int maxMessageSize,
            long probeInterval,
            long probeTimeout)
        {
            var parameters = Definition.Parameters(provider, port, name, processorPoolSize, maxBufferPoolSize, maxMessageSize, probeInterval, probeTimeout);

            var channel =
                stage.ActorFor<IServerRequestResponseChannel>(
                    Definition.Has<ServerRequestResponseChannelActor>(parameters, mailboxName, address.Name),
                    address, stage.World.DefaultLogger);

            return channel;
        }
    }
}