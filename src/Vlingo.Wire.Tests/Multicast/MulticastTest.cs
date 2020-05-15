// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Actors.TestKit;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Multicast
{
    public class MulticastTest
    {
        [Fact]
        public void TestMulticastPublishSubscribe()
        {
            var publisherCount = 0;
            var subscriberCount = 0;
            var accessSafely = AccessSafely.Immediately()
                .WritingWith<int>("publisherCount", (value) => publisherCount += value)
                .ReadingWith("publisherCount", () => publisherCount)
                .WritingWith<int>("subscriberCount", (value) => subscriberCount += value)
                .ReadingWith("subscriberCount", () => subscriberCount);
            
            var publisherConsumer = new MockChannelReaderConsumer("publisherCount");
            publisherConsumer.UntilConsume = accessSafely;

            var publisher = new MulticastPublisherReader(
                "test-publisher",
                new Group("237.37.37.1", 37771),
                37779,
                1024,
                publisherConsumer,
                ConsoleLogger.TestInstance());
            
            var subscriber = new MulticastSubscriber(
                "test-subscriber",
                new Group("237.37.37.1", 37771),
                1024,
                10,
                ConsoleLogger.TestInstance());
            
            var subscriberConsumer = new MockChannelReaderConsumer("subscriberCount");
            subscriberConsumer.UntilConsume = accessSafely;
            subscriber.OpenFor(subscriberConsumer);
            
            for (var idx = 0; idx < 10; ++idx)
            {
                publisher.SendAvailability();
            }
            
            publisher.ProcessChannel();

            for (var i = 0; i < 2; ++i)
            {
                subscriber.ProbeChannel();
            }

            subscriberConsumer.UntilConsume.ReadFromExpecting("subscriberCount", 10, 10000);
    
            Assert.Equal(0, publisherCount);
            Assert.Equal(10, subscriberCount);
        }

        [Fact]
        public void TestPublisherChannelReader()
        {
            var publisherCount = 0;
            var accessSafely = AccessSafely.AfterCompleting(1)
                .WritingWith<int>("publisherCount", value => publisherCount += value)
                .ReadingWith("publisherCount", () => publisherCount);
            
            var publisherConsumer = new MockChannelReaderConsumer("publisherCount");
            publisherConsumer.UntilConsume = accessSafely;

            var publisher = new MulticastPublisherReader(
                "test-publisher",
                new Group("237.37.37.2", 37381),
                37389,
                1024,
                publisherConsumer,
                ConsoleLogger.TestInstance());
            
            var socketWriter = new SocketChannelWriter(
                Address.From(
                    Host.Of("localhost"), 
                    37389, 
                    AddressType.Main),
                ConsoleLogger.TestInstance());

            socketWriter.Write(RawMessage.From(1, 1, "test-response"), new MemoryStream());

            publisher.ProcessChannel();
            
            Assert.Equal(1, publisherCount);
        }
        
        [Fact]
        public void TestPublisherChannelReaderWithMultipleClients()
        {
            var publisherCount = 0;
            var accessSafely = AccessSafely.AfterCompleting(4)
                .WritingWith<int>("publisherCount", (value) => publisherCount += value)
                .ReadingWith("publisherCount", () => publisherCount);
            
            var publisherConsumer = new MockChannelReaderConsumer("publisherCount");
            publisherConsumer.UntilConsume = accessSafely;

            var publisher = new MulticastPublisherReader(
                "test-publisher",
                new Group("237.37.37.2", 37391),
                37399,
                1024,
                publisherConsumer,
                ConsoleLogger.TestInstance());
            
            var client1 = new SocketChannelWriter(
                Address.From(
                    Host.Of("localhost"), 
                    37399, 
                    AddressType.Main),
                ConsoleLogger.TestInstance());
            
            var client2 = new SocketChannelWriter(
                Address.From(
                    Host.Of("localhost"), 
                    37399, 
                    AddressType.Main),
                ConsoleLogger.TestInstance());
            
            var client3 = new SocketChannelWriter(
                Address.From(
                    Host.Of("localhost"), 
                    37399, 
                    AddressType.Main),
                ConsoleLogger.TestInstance());

            client1.Write(RawMessage.From(1, 1, "test-response1"), new MemoryStream());
            client2.Write(RawMessage.From(1, 1, "test-response2"), new MemoryStream());
            client3.Write(RawMessage.From(1, 1, "test-response3"), new MemoryStream());
            client1.Write(RawMessage.From(1, 1, "test-response1"), new MemoryStream());
            
            publisher.ProbeChannel();
            publisher.ProbeChannel();
            publisher.ProbeChannel();
            publisher.ProbeChannel();

            Assert.Equal(4, publisherCount);
        }

        public MulticastTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
        }
    }
}