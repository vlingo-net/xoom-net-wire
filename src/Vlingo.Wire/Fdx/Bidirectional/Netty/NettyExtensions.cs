// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using Vlingo.Actors;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty
{
    public static class NettyExtensions
    {
        public static IAsyncResult BeginConnect(this Bootstrap bootstrap, string hostName, int port,
            AsyncCallback callback, object state) => bootstrap.ConnectAsync(hostName, port).ToApm(callback, state);

        public static IChannel EndConnect(this Bootstrap _, IAsyncResult asyncResult) => ((Task<IChannel>) asyncResult).Result;
    }
}