// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Xoom.Wire.Nodes;

public sealed class Address : IComparable<Address>
{
    private readonly Host _host;
    private readonly int _port;
    private readonly AddressType _type;
        
    public static string NoHost => "?";
    public static int NoPort => -1;
    public static Address NoNodeAddress { get; } = new Address(Host.Of(NoHost), NoPort, AddressType.None);
        
    public Address(Host host, int port, AddressType type)
    {
        _host = host;
        _port = port;
        _type = type;
    }

    public static Address From(string fullAddress, AddressType type)
    {
        var lastColon = fullAddress.LastIndexOf(":", StringComparison.Ordinal);
        if (lastColon == -1)
        {
            throw new ArgumentException($"The address is not valid: {fullAddress}", fullAddress);
        }
        return new Address(Host.Of(fullAddress.Substring(0, lastColon)), int.Parse(fullAddress.Substring(lastColon + 1)), type);
    }

    public static Address From(Host host, int port, AddressType type) => new Address(host, port, type);

    public string Full => $"{_host.Name}:{_port}";

    public Host Host => _host;

    public string HostName => _host.Name;

    public bool HasNoAddress => _host.Name.Equals(NoHost) || _port == NoPort;

    public bool IsValid => !HasNoAddress;

    public int Port => _port;

    public AddressType Type => _type;

    public int CompareTo(Address? other)
    {
        if (other == null || other.GetType() != typeof(Address))
        {
            return 1;
        }

        return _host.CompareTo(other._host);
    }
        
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != typeof(Address))
        {
            return false;
        }

        return _host.Equals(((Address)obj).Host);
    }

    public override int GetHashCode() => 31 * _host.GetHashCode();
        
    public override string ToString()
    {
        return $"Address[{_host},{_port},{_type}";
    }
}