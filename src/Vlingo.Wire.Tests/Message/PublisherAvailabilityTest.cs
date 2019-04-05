// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Message
{
    public class PublisherAvailabilityTest
    {
        private string textMessage = "PUB\nnm=test-dir addr=1.2.3.4:111";
        
        [Fact]
        public void TestMessage()
        {
            var publisherAvailability = new PublisherAvailability("test-dir", "1.2.3.4", 111);
            
            Assert.Equal(publisherAvailability, PublisherAvailability.From(textMessage));
        }
        
        [Fact]
        public void TestValidity()
        {
            var publisherAvailability = new PublisherAvailability("test-dir", "1.2.3.4", 111);
            
            Assert.True(publisherAvailability.IsValid);
            Assert.False(PublisherAvailability.From("blah").IsValid);
            Assert.True(PublisherAvailability.From(textMessage).IsValid);
        }
        
        [Fact]
        public void TestToAddress()
        {
            var publisherAvailability = new PublisherAvailability("test-dir", "1.2.3.4", 111);
            
            Assert.Equal(Address.From(Host.Of("1.2.3.4"), 111, AddressType.Main), publisherAvailability.ToAddress());
            Assert.Equal(Address.From(Host.Of("1.2.3.4"), 111, AddressType.Op), publisherAvailability.ToAddress(AddressType.Op));
        }

        public PublisherAvailabilityTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
        }
    }
}