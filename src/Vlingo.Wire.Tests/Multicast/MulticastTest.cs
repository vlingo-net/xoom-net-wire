// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Multicast;
using Xunit;

namespace Vlingo.Wire.Tests.Multicast
{
    public class MulticastTest
    {
        [Fact]
        public async Task TestMulticastPublishSubscribe()
        {
            var publisherConsumer = new MockChannelReaderConsumer();

            var publisher = new MulticastPublisherReader(
                "test-publisher",
                new Group("237.37.37.1", 37371),
                37379,
                1024,
                publisherConsumer,
                ConsoleLogger.TestInstance());
            
            var subscriber = new MulticastSubscriber(
                "test-subscriber",
                new Group("237.37.37.1", 37371),
                1024,
                10,
                ConsoleLogger.TestInstance());
            
            var subscriberConsumer = new MockChannelReaderConsumer();
            subscriber.OpenFor(subscriberConsumer);
            
            for (int idx = 0; idx < 10; ++idx)
            {
                publisher.SendAvailability();
            }
            
            publisher.ProcessChannel();
    
            for (int i = 0; i < 2; ++i)
            {
                subscriber.ProbeChannel();
            }
    
            Assert.Equal(0, publisherConsumer.ConsumeCount);
            Assert.Equal(10, subscriberConsumer.ConsumeCount);
        }
    }
}