// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Xoom.Wire.Node
{
    public sealed class AddressType : IComparable<AddressType>
    {
        public static readonly AddressType Main = new AddressType("addr=", false, false, true, false);
        public static readonly AddressType Op = new AddressType("op=", false, true, false, false);
        public static readonly AddressType App = new AddressType("app=", true, false, false, false);
        public static readonly AddressType None = new AddressType("", false, false, false, true);

        private AddressType(string field, bool application, bool operational, bool main, bool none)
        {
            Field = field;
            IsApplication = application;
            IsOperational = operational;
            IsMain = main;
            IsNone = none;
        }
        
        public string Field { get; }
        
        public bool IsApplication { get; }
        
        public bool IsOperational { get; }
        
        public bool IsMain { get; }
        
        public bool IsNone { get; }
        
        public int CompareTo(AddressType? other)
        {
            if (other == null || other.GetType() != typeof(AddressType))
            {
                return 1;
            }
            return string.Compare(Field, other.Field, StringComparison.Ordinal);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(AddressType))
            {
                return false;
            }

            return Field.Equals(((AddressType)obj).Field);
        }

        public override int GetHashCode() => 31 * Field.GetHashCode();

        public override string ToString() => $"AddressType[{Field}]";
    }
}