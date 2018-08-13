// Copyright (c) 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Vlingo.Wire.Node
{
    public sealed class Id : IComparable<Id>
    {
        public static readonly short UndefinedId = -1;
        public static readonly Id NoId = Id.Of(UndefinedId);

        private readonly short _value;

        public Id(short id)
        {
            _value = id;
        }

        public Id(int id)
        {
            _value = (short)id;
        }

        public static Id Of(short id)
        {
            return new Id(id);
        }

        public Collection<Id> Collected()
        {
            return new Collection<Id>() { this };
        }

        public bool HasNoId()
        {
            return _value == UndefinedId;
        }

        public short Value()
        {
            return _value;
        }

        public string ValueString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        public bool IsValid()
        {
            return !HasNoId();
        }

        public int ToInteger()
        {
            return (int)_value;
        }

        public bool Equals(Id other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj.GetType() != this.GetType()) return false;
            if (ReferenceEquals(this, obj)) return true;

            return Equals((Id)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public int CompareTo(Id other)
        {
            return _value.CompareTo(other._value);
        }

        public bool GreaterThan(Id other)
        {
            return _value > other._value;
        }
    }
}
