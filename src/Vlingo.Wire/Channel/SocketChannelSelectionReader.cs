// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class SocketChannelSelectionReader: SelectionReader
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _readDone;

        public SocketChannelSelectionReader(ChannelMessageDispatcher dispatcher, ILogger logger) : base(dispatcher)
        {
            _logger = logger;
            _readDone = new SemaphoreSlim(1);
        }

        public override void Read(Socket channel, RawMessageBuilder builder)
        {
            _readDone.Wait();
            
            var buffer = builder.WorkBuffer();
            var bytes = new byte[buffer.Length];
            var state = new StateObject(channel, buffer, bytes, builder);
            channel.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, ReceiveCallback, state);
            Dispatcher.DispatchMessageFor(builder);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var state = ar.AsyncState as StateObject;
                var client = state?.WorkSocket;
                var buffer = state?.Buffer;
                var bytes = state?.Bytes;
                var builder = state?.Builder;

                var bytesRead = client?.EndReceive(ar);

                if (bytesRead.HasValue && bytesRead.Value > 0 && state != null && bytes != null)
                {
                    buffer?.Write(bytes, state.TotalRead, bytesRead.Value);
                    state.TotalRead += bytesRead.Value;
                }

                var bytesRemain = client?.Available;
                if (bytesRemain > 0 && bytes != null)
                {
                    client?.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, ReceiveCallback, state);
                }
                else
                {
                    if (bytesRead > 0)
                    {
                        Dispatcher.DispatchMessageFor(builder);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error while receiving data", e);
            }
            finally
            {
                _readDone.Release();
            }
        }
        
        private class StateObject
        {
            public StateObject(Socket workSocket, Stream buffer, byte[] bytes, RawMessageBuilder builder)
            {
                WorkSocket = workSocket;
                Buffer = buffer;
                Bytes = bytes;
                Builder = builder;
            }
            
            public Socket WorkSocket { get; }
            
            public Stream Buffer { get; }
            
            public byte[] Bytes { get; }
            
            public RawMessageBuilder Builder { get; }
            
            public int TotalRead { get; set; }
        }
    }
}