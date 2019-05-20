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

        public void Broadcast(RawMessage message)
        {
            var buffer = _pool.Access();
            Broadcast(BytesFrom(message, buffer));
        }

        public void Broadcast(IConsumerByteBuffer buffer)
        {
            // currently based on configured nodes,
            // but eventually could be live-node based
            Broadcast(_provider.AllOtherNodeChannels, buffer);
        }

        public void Broadcast(IEnumerable<Node> selectNodes, RawMessage message)
        {
            var buffer = _pool.Access();
            Broadcast(selectNodes, BytesFrom(message, buffer));
        }

        public void Broadcast(IEnumerable<Node> selectNodes, IConsumerByteBuffer buffer)
        {
            Broadcast(_provider.ChannelsFor(selectNodes), buffer);
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

        public void SendTo(RawMessage message, Id id)
        {
            var buffer = _pool.Access();
            SendTo(BytesFrom(message, buffer), id);
        }

        public async void SendTo(IConsumerByteBuffer buffer, Id id)
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

        private async void Broadcast(IReadOnlyDictionary<Id, IManagedOutboundChannel> channels, IConsumerByteBuffer buffer)
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