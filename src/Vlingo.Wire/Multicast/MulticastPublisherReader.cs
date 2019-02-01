// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Multicast
{
    public class MulticastPublisherReader : ChannelMessageDispatcher, IChannelPublisher
    {
        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void ProcessChannel()
        {
            throw new System.NotImplementedException();
        }

        public void SendAvailability()
        {
            throw new System.NotImplementedException();
        }

        public void Send(RawMessage message)
        {
            throw new System.NotImplementedException();
        }

        public override IChannelReaderConsumer Consumer { get; }

        public override ILogger Logger { get; }

        public override string Name { get; }
    }
}
