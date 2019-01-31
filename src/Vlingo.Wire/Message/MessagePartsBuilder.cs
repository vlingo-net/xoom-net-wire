// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Vlingo.Wire.Message
{
    using Vlingo.Wire.Node;
    
    public static class MessagePartsBuilder
    {
        public static Address AddressFromRecord(string record, AddressType type)
        {
            var text = ParseField(record, type.Field);
            
            if (text == null)
            {
                return Address.NoNodeAddress;
            }

            return Address.From(text, type);
        }

        public static IEnumerable<Address> AddressesFromRecord(string record, AddressType type)
        {
            var addresses = new HashSet<Address>();
            
            var parts = record.Split('\n');

            if (parts.Length < 3)
            {
                return addresses;
            }

            for (int index = 2; index < parts.Length; ++index)
            {
                addresses.Add(AddressFromRecord(parts[index], type));
            }

            return addresses;
        }

        public static IEnumerable<Node> NodesFrom(string content)
        {
            var nodeEntries = new HashSet<Node>();

            var parts = content.Split('\n');

            if (parts.Length < 3)
            {
                return nodeEntries;
            }

            for (int index = 2; index < parts.Length; ++index)
            {
                nodeEntries.Add(NodeFromRecord(parts[index]));
            }

            return nodeEntries;
        }

        public static Node NodeFrom(string content)
        {
            var parts = content.Split('\n');

            if (parts.Length < 2)
            {
                return Node.NoNode;
            }

            return NodeFromRecord(parts[1]);
        }

        public static Node NodeFromRecord(string record)
        {
            var id = IdFromRecord(record);
            var name = NameFromRecord(record);
            var opAddress = AddressFromRecord(record, AddressType.Op);
            var appAddress = AddressFromRecord(record, AddressType.App);

            return new Node(id, name, opAddress, appAddress);
        }

        public static Id IdFrom(string content)
        {
            var parts = content.Split('\n');

            if (parts.Length < 2)
            {
                return Id.NoId;
            }

            return IdFromRecord(parts[1]);
        }

        public static Id IdFromRecord(string record)
        {
            var text = ParseField(record, "id=");

            if (text == null)
            {
                return Id.NoId;
            }

            return Id.Of(short.Parse(text));
        }

        public static Name NameFrom(string content)
        {
            var parts = content.Split('\n');

            if (parts.Length < 2)
            {
                return Name.NoNodeName;
            }

            return NameFromRecord(parts[1]);
        }

        public static Name NameFromRecord(string record)
        {
            var text = ParseField(record, "nm=");

            if (text == null)
            {
                return Name.NoNodeName;
            }

            return new Name(text);
        }

        public static string ParseField(string record, string fieldName)
        {
            var skinnyRecord = record.Trim();

            var idIndex = skinnyRecord.IndexOf(fieldName, StringComparison.InvariantCulture);

            if (idIndex == -1)
            {
                return null;
            }
            
            var valueIndex = idIndex + fieldName.Length;
            var space = skinnyRecord.IndexOf(' ', valueIndex);
            
            if (valueIndex >= space) 
            {
                return skinnyRecord.Substring(valueIndex);
            }
            
            return skinnyRecord.Substring(valueIndex, space - valueIndex);
        }
    }
}