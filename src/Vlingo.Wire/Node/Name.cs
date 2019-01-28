// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Wire.Node
{
    public sealed class Name : IComparable<Name>
    {
        public static string NoName { get; } = "?";
        public static Name NoNodeName { get; } = new Name(NoName);

        public static Name Of(string name)
        {
            return new Name(name);
        }
        
        public string Value { get; }

        public Name(string name)
        {
            Value = name;
        }
        
        public bool HasNoName => Value == NoName;

        public bool SameAs(string name)
        {
            return Value == name;
        }

        public int CompareTo(Name other)
        {
            if (other == null || other.GetType() != typeof(Name))
            {
                return 1;
            }

            return String.Compare(Value, other.Value, StringComparison.InvariantCulture);
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Name))
            {
                return false;
            }

            return Value.Equals(((Name)obj).Value);
        }

        public override int GetHashCode() => 31 * Value.GetHashCode();
        
        public override string ToString()
        {
            return $"Name[{Value}]";
        }
    }
}