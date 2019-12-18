// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Common;
using Vlingo.Common.Pool;

namespace Vlingo.Wire.Message
{
    public class ConsumerByteBufferPool : ElasticResourcePool<IConsumerByteBuffer, Nothing>
    {
        public ConsumerByteBufferPool(Config config, int maxBufferSize) : base(config, new ConsumerByteBufferFactory(maxBufferSize))
        {
        }

        public override IConsumerByteBuffer Acquire() => SetPool(base.Acquire());

        public override IConsumerByteBuffer Acquire(Nothing arguments) => SetPool(base.Acquire(arguments));
        
        private IConsumerByteBuffer SetPool(IConsumerByteBuffer buffer)
        {
            if (buffer is PoolAwareConsumerByteBuffer poolAwareConsumerByteBuffer)
            {
                poolAwareConsumerByteBuffer.SetPool(this);
            }
            return buffer;
        }

        private sealed class ConsumerByteBufferFactory : IResourceFactory<IConsumerByteBuffer, Nothing>
        {
            private readonly AtomicInteger _idSequence = new AtomicInteger(0);
            
            private readonly int _maxBufferSize;

            public ConsumerByteBufferFactory(int maxBufferSize) => _maxBufferSize = maxBufferSize;

            public IConsumerByteBuffer Create(Nothing arguments) => new PoolAwareConsumerByteBuffer(_idSequence.IncrementAndGet(), _maxBufferSize);

            public IConsumerByteBuffer Reset(IConsumerByteBuffer resource, Nothing arguments) => resource.Clear();

            public void Destroy(IConsumerByteBuffer resource)
            {
            }

            public Nothing DefaultArguments { get; } = Nothing.AtAll;
        }
        
        private sealed class PoolAwareConsumerByteBuffer : BasicConsumerByteBuffer
        {
            private ConsumerByteBufferPool _pool;
            
            public PoolAwareConsumerByteBuffer(int id, int maxBufferSize) : base(id, maxBufferSize)
            {
            }

            public void SetPool(ConsumerByteBufferPool pool) => _pool = pool;

            public override void Release() => _pool.Release(this);

            public override string ToString() => $"PooledByteBuffer[id={Id}]";
        }
    }
}