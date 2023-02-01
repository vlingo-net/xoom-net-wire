// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Text;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Message;

public class PublisherAvailability
{
    public static string TypeName => "PUB";

    private readonly string _host;
    private readonly string _name;
    private readonly int _port;

    public PublisherAvailability(string name, string host, int port)
    {
        _name = name;
        _host = host;
        _port = port;
    }

    public static PublisherAvailability From(string content)
    {
        if (content.StartsWith(TypeName))
        {
            var name = MessagePartsBuilder.NameFrom(content);
            var type = AddressType.Main;
            var address = MessagePartsBuilder.AddressFromRecord(content, type);
            return new PublisherAvailability(name.Value, address.Host.Name, address.Port);
        }
        return new PublisherAvailability(Name.NoName, "", 0);
    }

    public bool IsValid => !_name.Equals(Name.NoName);

    public Address ToAddress() => ToAddress(AddressType.Main);
        
    public Address ToAddress(AddressType type) => Address.From(Host.Of(_host), _port, type);

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != typeof(PublisherAvailability))
        {
            return false;
        }

        var otherPa = (PublisherAvailability)obj;

        return _name.Equals(otherPa._name) && _host.Equals(otherPa._host) && _port == otherPa._port;
    }

    public override int GetHashCode() => 31 * (_name.GetHashCode() + _host.GetHashCode() + _port);

    public override string ToString()
    {
        var builder = new StringBuilder();
    
        builder
            .Append("PUB\n")
            .Append("nm=").Append(_name)
            .Append(" addr=").Append(_host)
            .Append(":").Append(_port);
    
        return builder.ToString();
    }
}