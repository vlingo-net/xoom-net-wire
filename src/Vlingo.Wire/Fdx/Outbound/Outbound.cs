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

        public async Task BroadcastAsync(RawMessage message)
        {
            var buffer = _pool.Access();
            await BroadcastAsync(BytesFrom(message, buffer));
        }

        public async Task BroadcastAsync(IConsumerByteBuffer buffer)
        {
            // currently based on configured nodes,
            // but eventually could be live-node based
            await BroadcastAsync(_provider.AllOtherNodeChannels, buffer);
        }

        public async Task BroadcastAsync(IEnumerable<Node> selectNodes, RawMessage message)
        {
            var buffer = _pool.Access();
            await BroadcastAsync(selectNodes, BytesFrom(message, buffer));
        }

        public async Task BroadcastAsync(IEnumerable<Node> selectNodes, IConsumerByteBuffer buffer)
        {
            await BroadcastAsync(_provider.ChannelsFor(selectNodes), buffer);
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

        public async Task SendToAsync(RawMessage message, Id id)
        {
            var buffer = _pool.Access();
            await SendToAsync(BytesFrom(message, buffer), id);
        }

        public async Task SendToAsync(IConsumerByteBuffer buffer, Id id)
        {
            try 
            {
                Open(id);
                await _provider.ChannelFor(id).WriteAsync(buffer.AsStream());
            }
            finally
            {
                buffer.Release();
            }
        }

        private async Task BroadcastAsync(IReadOnlyDictionary<Id, IManagedOutboundChannel> channels, IConsumerByteBuffer buffer)
        {
            try
            {
                var bufferToWrite = buffer.AsStream();
                foreach (var channel in channels.Values)
                {
                    bufferToWrite.Position = 0;
                    await channel.WriteAsync(bufferToWrite);
                }
            }
            finally
            {
                buffer.Release();
            }
        }
    }
}