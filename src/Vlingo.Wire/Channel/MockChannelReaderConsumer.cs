// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class MockChannelReaderConsumer : IChannelReaderConsumer
    {
        private readonly string _name;
        private readonly List<string> _messages = new List<string>();

        public MockChannelReaderConsumer(string name)
        {
            _name = name;
        }

        public void Consume(RawMessage message)
        {
            _messages.Add(message.AsTextMessage());
            UntilConsume?.WriteUsing(_name, 1);
        }

        public IReadOnlyCollection<string> Messages => _messages;
        
        public AccessSafely? UntilConsume { get; set; }
    }
}
