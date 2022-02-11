// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Common;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Tests.Message;

namespace Vlingo.Xoom.Wire.Tests.Channel;

public class MockChannelReader: AbstractMessageTool, IChannelReader
{
    private IChannelReaderConsumer _consumer;
        
    public MockChannelReader()
    {
        ProbeChannelCount = new AtomicInteger(0);
    }
        
    public static readonly string MessagePrefix = "Message-";

    public AtomicInteger ProbeChannelCount { get; }
        
    public void Close()
    {
    }

    public string Name { get; } = "mock";
        
    public int Port { get; } = 0;
        
    public void OpenFor(IChannelReaderConsumer consumer)
    {
        _consumer = consumer;
    }

    public void ProbeChannel()
    {
        ProbeChannelCount.IncrementAndGet();
            
        var message = RawMessage.From(0, 0, MessagePrefix + ProbeChannelCount.Get());
            
        _consumer.Consume(message);
    }
}