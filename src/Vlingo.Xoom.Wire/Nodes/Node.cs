// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Vlingo.Xoom.Wire.Nodes
{
    public sealed class Node : IComparable<Node>
    {
        public static Node NoNode { get; } = new Node(Id.NoId, Name.NoNodeName, Address.NoNodeAddress, Address.NoNodeAddress);

        public static Node With(Id id, Name name, Host host, int operationalPort, int applicationPort)
        {
            var operationalAddress = new Address(host, operationalPort, AddressType.Op);
            var applicationAddress = new Address(host, applicationPort, AddressType.App);
            
            return new Node(id, name, operationalAddress, applicationAddress);
        }

        public Node(Id id, Name name, Address operationalAddress, Address applicationAddress)
        {
            Id = id;
            Name = name;
            OperationalAddress = operationalAddress;
            ApplicationAddress = applicationAddress;
        }
        
        public Id Id { get; }
        
        public Name Name { get; }
        
        public Address OperationalAddress { get; }
        
        public Address ApplicationAddress { get; }
        
        public IEnumerable<Node> Collected => new[] {this};

        public bool HasMissingPart => Id.HasNoId &&
                                      Name.HasNoName &&
                                      OperationalAddress.HasNoAddress &&
                                      ApplicationAddress.HasNoAddress;

        public bool IsValid => !HasMissingPart;

        public bool IsLeaderOver(Id nodeId) => IsValid && Id.GreaterThan(nodeId);

        public bool GreaterThan(Node other) => Id.GreaterThan(other.Id);
        
        public int CompareTo(Node? other)
        {
            if (other == null || other.GetType() != typeof(Node))
            {
                return 1;
            }

            var result = Id.CompareTo(other.Id);
            if (result != 0)
            {
                return result;
            }

            result = Name.CompareTo(other.Name);
            if (result != 0)
            {
                return result;
            }

            result = OperationalAddress.CompareTo(other.OperationalAddress);
            if (result != 0)
            {
                return result;
            }
            
            result = ApplicationAddress.CompareTo(other.ApplicationAddress);
            if (result != 0)
            {
                return result;
            }
            
            return 0;
        }
        
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(Node))
            {
                return false;
            }

            var node = (Node) obj;

            return
                Id.Equals(node.Id) &&
                Name.Equals(node.Name) &&
                OperationalAddress.Equals(node.OperationalAddress) &&
                ApplicationAddress.Equals(node.ApplicationAddress);
        }

        public override int GetHashCode() => 31 * (Id.GetHashCode() + Name.GetHashCode() + OperationalAddress.GetHashCode() + ApplicationAddress.GetHashCode());

        public override string ToString()
        {
            return $"Node[{Id},{Name},{OperationalAddress},{ApplicationAddress}]";
        }
    }
}