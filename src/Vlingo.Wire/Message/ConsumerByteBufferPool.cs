// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics;
using Vlingo.Common;
using Vlingo.Common.Pool;

namespace Vlingo.Wire.Message
{
    public class ConsumerByteBufferPool : ElasticResourcePool<IConsumerByteBuffer, string>
    {
        public ConsumerByteBufferPool(Config config, int maxBufferSize) : base(config, new ConsumerByteBufferFactory(maxBufferSize))
        {
        }

        public override IConsumerByteBuffer Acquire() => SetPool(base.Acquire());

        public override IConsumerByteBuffer Acquire(string arguments) => SetPool(base.Acquire(arguments));
        
        private IConsumerByteBuffer SetPool(IConsumerByteBuffer buffer)
        {
            if (buffer is PoolAwareConsumerByteBuffer poolAwareConsumerByteBuffer)
            {
                poolAwareConsumerByteBuffer.SetPool(this);
            }
            return buffer;
        }

        private sealed class ConsumerByteBufferFactory : IResourceFactory<IConsumerByteBuffer, string>
        {
            private readonly AtomicInteger _idSequence = new AtomicInteger(0);
            
            private readonly int _maxBufferSize;

            public ConsumerByteBufferFactory(int maxBufferSize) => _maxBufferSize = maxBufferSize;

            public IConsumerByteBuffer Create(string arguments)
            {
                var poolAwareConsumerByteBuffer = new PoolAwareConsumerByteBuffer(_idSequence.IncrementAndGet(), _maxBufferSize);
                poolAwareConsumerByteBuffer.Tag = arguments;
                return poolAwareConsumerByteBuffer;
            }

            public IConsumerByteBuffer Reset(IConsumerByteBuffer buffer, string arguments)
            {
                if (buffer is BasicConsumerByteBuffer basicConsumerByteBuffer)
                {
                    basicConsumerByteBuffer.Tag = arguments;
                }
                
                return buffer.Clear();
            }

            public void Destroy(IConsumerByteBuffer resource)
            {
            }

            public string DefaultArguments { get; } = "notag";
        }
        
        private sealed class PoolAwareConsumerByteBuffer : BasicConsumerByteBuffer
        {
            private ConsumerByteBufferPool? _pool;
            private readonly AtomicBoolean _active = new AtomicBoolean(true);
            
            public PoolAwareConsumerByteBuffer(int id, int maxBufferSize) : base(id, maxBufferSize)
            {
            }

            public void SetPool(ConsumerByteBufferPool pool) => _pool = pool;

            public void Activate() => _active.Set(true);

            public override void Release()
            {
                if (_active.CompareAndSet(true, false))
                {
                    _pool?.Release(this);
                }
                else
                {
                    Debug.WriteLine($"Attempt to release the same buffer [{Id}:{Tag}] more than once");
                }
                _pool?.Release(this);
            }

            public override string ToString() => $"PooledByteBuffer[id={Id}]";
        }
    }
}