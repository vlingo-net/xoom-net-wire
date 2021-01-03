// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class MockChannelReaderConsumer : IChannelReaderConsumer
    {
        private readonly List<string> _messages = new List<string>();
        private AccessSafely _access = AccessSafely.AfterCompleting(0);
        private readonly AtomicInteger _count = new AtomicInteger(0);

        public void Consume(RawMessage message)
        {
            _messages.Add(message.AsTextMessage());
            _access.WriteUsing("count", 1);
        }
        
        public AccessSafely AfterCompleting(int times)
        {
            _access =
                AccessSafely.AfterCompleting(times)
                    .WritingWith<int>("count", value => _count.IncrementAndGet())
                    .ReadingWith("count", () => _count.Get());

            return _access;
        }

        public IReadOnlyCollection<string> Messages => _messages;
    }
}
