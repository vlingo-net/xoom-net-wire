// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Xoom.Wire.Nodes
{
    public sealed class Host : IComparable<Host>
    {
        public static string NoName => "?";
        public static Host NoHostName { get; } = new Host(NoName);
        
        public string Name { get; }

        public Host(string name) => Name = name;

        public static Host Of(string name) => new Host(name);
        
        public bool HasNoName => Name == NoName;

        public bool SameAs(string name) => Name == name;

        public int CompareTo(Host? other)
        {
            if (other == null || other.GetType() != typeof(Host))
            {
                return 1;
            }

            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(Host))
            {
                return false;
            }

            return Name.Equals(((Host)obj).Name);
        }

        public override int GetHashCode() => 31 * Name.GetHashCode();
        
        public override string ToString() => $"Host[{Name}]";
    }
}