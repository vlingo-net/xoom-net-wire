// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Net.Sockets;

namespace Vlingo.Wire.Channel
{
    public static class SocketExtensions
    {
        public static bool IsSocketConnected(this Socket s, int probeTimeout = 1)
        {
            return !(s.Poll(probeTimeout * 1000, SelectMode.SelectRead) && s.Available == 0 || !s.Connected);
        }
    }
}