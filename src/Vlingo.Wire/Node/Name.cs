//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

namespace Vlingo.Wire.Node
{
    public sealed class Name
    {
        public static string NoName { get; } = "?";
        public static Name NoNodeName { get; } = new Name(NoName);
        
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