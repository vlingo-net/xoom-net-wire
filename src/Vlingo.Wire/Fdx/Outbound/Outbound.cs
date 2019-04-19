// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading.Tasks;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Fdx.Outbound
{
    using Node;
    
    public class Outbound
    {
        private readonly ByteBufferPool _pool;
        private readonly IManagedOutboundChannelProvider _provider;

        public Outbound(IManagedOutboundChannelProvider provider, ByteBufferPool byteBufferPool)
        {
            _provider = provider;
            _pool = byteBufferPool;
        }

        public async Task Broadcast(RawMessage message)
        {
            var buffer = _pool.Access();
            await Broadcast(BytesFrom(message, buffer));
        }

        public async Task Broadcast(IConsumerByteBuffer buffer)
        {
            // currently based on configured nodes,
            // but eventually could be live-node based
            await Broadcast(_provider.AllOtherNodeChannels, buffer);
        }

        public async Task Broadcast(IEnumerable<Node> selectNodes, RawMessage message)
        {
            var buffer = _pool.Access();
            await Broadcast(selectNodes, BytesFrom(message, buffer));
        }

        public async Task Broadcast(IEnumerable<Node> selectNodes, IConsumerByteBuffer buffer)
        {
            await Broadcast(_provider.ChannelsFor(selectNodes), buffer);
        }

        public IConsumerByteBuffer BytesFrom(RawMessage message, IConsumerByteBuffer buffer)
        {
            message.CopyBytesTo(buffer.Clear().AsStream());
            return buffer.Flip();
        }

        public void Close() => _provider.Close();

        public void Close(Id id) => _provider.Close(id);

        public void Open(Id id) => _provider.ChannelFor(id);

        public ByteBufferPool.PooledByteBuffer PooledByteBuffer() => _pool.Access();

        public async Task SendTo(RawMessage message, Id id)
        {
            var buffer = _pool.Access();
            await SendTo(BytesFrom(message, buffer), id);
        }

        public async Task SendTo(IConsumerByteBuffer buffer, Id id)
        {
            try 
            {
                Open(id);
                await _provider.ChannelFor(id).Write(buffer.AsStream());
            }
            finally
            {
                buffer.Release();
            }
        }

        private async Task Broadcast(IReadOnlyDictionary<Id, IManagedOutboundChannel> channels, IConsumerByteBuffer buffer)
        {
            try
            {
                var bufferToWrite = buffer.AsStream();
                foreach (var channel in channels.Values)
                {
                    bufferToWrite.Position = 0;
                    await channel.Write(bufferToWrite);
                }
            }
            finally
            {
                buffer.Release();
            }
        }
    }
}