// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Net.Sockets;
using System.Threading.Tasks;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public class SocketChannelSelectionReader: SelectionReader
    {
        public SocketChannelSelectionReader(ChannelMessageDispatcher dispatcher) : base(dispatcher)
        {
        }

        public override async Task Read(Socket channel, RawMessageBuilder builder)
        {
            var bytesRead = 0;
            var totalBytesRead = 0;
            do
            {
                var buffer = builder.WorkBuffer();
                var bytes = new byte[buffer.Length];
                bytesRead = await channel.ReceiveAsync(bytes, SocketFlags.None);
                await buffer.WriteAsync(bytes, totalBytesRead, bytesRead);
                totalBytesRead += bytesRead;

            } while (channel.Available > 0);

            Dispatcher.DispatchMessageFor(builder);
    
            if (bytesRead == 0)
            {
                CloseClientResources(channel);
            }
        }
    }
}