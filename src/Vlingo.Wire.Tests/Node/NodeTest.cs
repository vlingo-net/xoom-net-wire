// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Node
{
    using Vlingo.Wire.Node;
    
    public class NodeTest
    {
        [Fact]
        public void TestNodeCreationState()
        {
            var id1 = Id.Of(1);
            var name1 = new Name("name1");
            var opAddress1 = new Address(Host.Of("localhost"), 11111, AddressType.Op);
            var appAddress1 = new Address(Host.Of("localhost"), 11112, AddressType.App);
            var node1 = new Node(id1, name1, opAddress1, appAddress1);
            
            Assert.False(node1.HasMissingPart);
            Assert.True(node1.IsValid);
            
            var id2 = Id.Of(2);
            var name2 = new Name("name2");
            var opAddress2 = new Address(Host.Of("localhost"), 11113, AddressType.Op);
            var appAddress2 = new Address(Host.Of("localhost"), 11114, AddressType.App);
            var node2 = new Node(id2, name2, opAddress2, appAddress2);
            
            Assert.False(node2.HasMissingPart);
            Assert.True(node2.IsValid);
            
            Assert.True(node2.IsLeaderOver(node1.Id));
            
            Assert.False(node1.GreaterThan(node2));
            
            Assert.Equal(-1, node1.CompareTo(node2));
            Assert.Equal(1, node2.CompareTo(node1));
        }

        public NodeTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
        }
    }
}