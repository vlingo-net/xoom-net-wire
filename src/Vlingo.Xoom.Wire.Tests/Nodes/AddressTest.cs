// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Wire.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Xoom.Wire.Tests.Nodes;

public class AddressTest
{
    [Fact]
    public void TestAddressCreationState()
    {
        var address = Address.From(Host.Of("localhost"), 11111, AddressType.Op);
        Assert.True(address.IsValid);
        Assert.False(address.HasNoAddress);
        Assert.Equal("localhost", address.HostName);
        Assert.Equal(11111, address.Port);
        Assert.Equal(AddressType.Op, address.Type);
    }
        
    [Fact]
    public void TestAddressFromText()
    {
        var address = Address.From("localhost:22222", AddressType.App);
        Assert.True(address.IsValid);
        Assert.False(address.HasNoAddress);
        Assert.Equal("localhost", address.HostName);
        Assert.Equal(22222, address.Port);
        Assert.Equal(AddressType.App, address.Type);
    }

    public AddressTest(ITestOutputHelper output)
    {
        var converter = new Converter(output);
        Console.SetOut(converter);
    }
}