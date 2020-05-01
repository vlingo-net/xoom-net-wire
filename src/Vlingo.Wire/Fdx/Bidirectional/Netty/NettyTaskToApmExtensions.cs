// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using Vlingo.Actors;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty
{
    public static class NettyTaskToApmExtensions
    {
        public static IAsyncResult BeginConnect(this Bootstrap bootstrap, string hostName, int port,
            AsyncCallback callback, object state)
        {
            if (hostName.IsLocalIpAddress())
            {
                return bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port)).ToApm(callback, state);
            }
            
            return bootstrap.ConnectAsync(hostName, port).ToApm(callback, state);
        }

        public static IChannel EndConnect(this Bootstrap _, IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<IChannel>) asyncResult).Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerException;
            } 
        }
        
        public static IAsyncResult BeginBind(this ServerBootstrap bootstrap, int port, AsyncCallback callback, object state)
            => bootstrap.BindAsync(new IPEndPoint(IPAddress.Any, port)).ToApm(callback, state);
        
        public static IChannel EndBind(this ServerBootstrap _, IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<IChannel>) asyncResult).Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerException;
            } 
        }
    }
}