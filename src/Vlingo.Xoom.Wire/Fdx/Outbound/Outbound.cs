// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Outbound
{
    public class Outbound
    {
        private readonly ConsumerByteBufferPool _pool;
        private readonly IManagedOutboundChannelProvider _provider;

        public Outbound(IManagedOutboundChannelProvider provider, ConsumerByteBufferPool byteBufferPool)
        {
            _provider = provider;
            _pool = byteBufferPool;
        }

        public void Broadcast(RawMessage message)
        {
            var buffer = _pool.Acquire();
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
            var buffer = _pool.Acquire();
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

        public IConsumerByteBuffer PooledByteBuffer() => _pool.Acquire();

        public void SendTo(RawMessage message, Id id)
        {
            var buffer = _pool.Acquire();
            SendTo(BytesFrom(message, buffer), id);
        }

        public void SendTo(IConsumerByteBuffer buffer, Id id)
        {
            try 
            {
                Open(id);
                _provider.ChannelFor(id).Write(buffer.AsStream());
            }
            finally
            {
                buffer.Release();
            }
        }

        private void Broadcast(IReadOnlyDictionary<Id, IManagedOutboundChannel> channels, IConsumerByteBuffer buffer)
        {
            try
            {
                var bufferToWrite = buffer.AsStream();
                foreach (var channel in channels.Values)
                {
                    bufferToWrite.Position = 0;
                    channel.Write(bufferToWrite);
                }
            }
            finally
            {
                buffer.Release();
            }
        }
    }
}