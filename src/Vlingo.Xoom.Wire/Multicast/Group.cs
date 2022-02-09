// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

namespace Vlingo.Xoom.Wire.Multicast
{
    public sealed class Group
    {
        public Group(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public string Address { get; }

        public int Port { get; }
    }
}