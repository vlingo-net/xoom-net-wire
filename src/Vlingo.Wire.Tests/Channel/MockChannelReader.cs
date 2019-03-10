// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using Vlingo.Common;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Tests.Message;

namespace Vlingo.Wire.Tests.Channel
{
    public class MockChannelReader: AbstractMessageTool, IChannelReader
    {
        private IChannelReaderConsumer _consumer;
        
        public static readonly string MessagePrefix = "Message-";
            
        public AtomicInteger ProbeChannelCount => new AtomicInteger(0);
        
        public void Close()
        {
        }

        public string Name { get; } = "mock";
        
        public void OpenFor(IChannelReaderConsumer consumer)
        {
            _consumer = consumer;
        }

        public Task ProbeChannelAsync()
        {
            ProbeChannelCount.IncrementAndGet();
            
            var message = RawMessage.From(0, 0, MessagePrefix + ProbeChannelCount.Get());
            
            _consumer.Consume(message);

            return Task.CompletedTask;
        }
    }
}