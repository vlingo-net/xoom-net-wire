// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class BasicClientRequestResponseChannel : ClientRequestResponseChannel
    {
        private readonly ManualResetEvent _connectDone;
        
        public BasicClientRequestResponseChannel(
            Address address,
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            ILogger logger) : base(address, consumer, maxBufferPoolSize, maxMessageSize, logger)
        {
            _connectDone = new ManualResetEvent(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connectDone.Dispose();
        }

        protected override Socket PreparedChannelDelegate()
        {
            var channel = Channel;
            try
            {
                if (channel != null)
                {
                    if (channel.IsSocketConnected())
                    {
                        PreviousPrepareFailures = 0;
                        return channel;
                    }

                    CloseChannel();
                }
                else
                {
                    channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    channel.BeginConnect(Address.HostName, Address.Port, ConnectCallback, channel);
                    _connectDone.WaitOne();
                    PreviousPrepareFailures = 0;
                    return channel;
                }
            }
            catch (Exception e)
            {
                CloseChannel();
                var message = $"{GetType().Name}: Cannot prepare/open channel because: {e.Message}";
                if (PreviousPrepareFailures == 0)
                {
                    Logger.Error(message, e);
                }
                else if (PreviousPrepareFailures % 20 == 0)
                {
                    Logger.Info($"AGAIN: {message}");
                }
            }
            ++PreviousPrepareFailures;
            return null;
        }
        
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Logger.Info($"Socket connected to {client.RemoteEndPoint}");

                // Signal that the connection has been made.  
                _connectDone.Set();
            }
            catch (Exception e)
            {
                Logger.Error("Cannot connect", e);
            }
        }
    }
}