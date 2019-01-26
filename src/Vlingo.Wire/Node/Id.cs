//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace Vlingo.Wire.Node
{
    public sealed class Id : IComparable<Id>
    {
        private const short UndefinedId = -1;
        public static Id NoId { get; } = Id.Of(UndefinedId);

        public short Value { get; }

        public Id(int id) : this((short)id)
        {
        }
        
        public Id(short id)
        {
            Value = id;
        }

        public static Id Of(int id)
        {
            return new Id(id);
        }

        public static Id Of(short id)
        {
            return new Id(id);
        }

        public IEnumerable<Id> Collected => new[] {this};

        public bool HasNoId => Value == UndefinedId;

        public bool IsValid => !HasNoId;

        public int CompareTo(Id other)
        {
            if (other == null || other.GetType() != typeof(Id))
            {
                return 1;
            }
            return Value.CompareTo(other.Value);
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Id))
            {
                return false;
            }

            return Value.Equals(((Id)obj).Value);
        }

        public override int GetHashCode() => Value.GetHashCode();
    }
}