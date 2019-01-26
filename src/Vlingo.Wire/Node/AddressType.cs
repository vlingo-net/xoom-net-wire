// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Wire.Node
{
    public sealed class AddressType : IComparable<AddressType>
    {
        public static readonly AddressType Main = new AddressType("addr=");
        public static readonly AddressType Op = new AddressType("op=");
        public static readonly AddressType App = new AddressType("app=");
        public static readonly AddressType None = new AddressType("");

        private AddressType(string field)
        {
            Field = field;
        }
        
        public string Field { get; }
        
        public int CompareTo(AddressType other)
        {
            if (other == null || other.GetType() != typeof(AddressType))
            {
                return 1;
            }
            return String.Compare(Field, other.Field, StringComparison.InvariantCulture);
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(AddressType))
            {
                return false;
            }

            return Field.Equals(((AddressType)obj).Field);
        }

        public override int GetHashCode() => 31 * Field.GetHashCode();
    }
}