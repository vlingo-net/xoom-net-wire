// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Common;

namespace Vlingo.Wire.Message
{
    public class ByteBufferPool
    {
        // TODO: This should be converted to ArrayPool<T>
        private readonly PooledByteBuffer[] _pool;
        private readonly int _poolSize;

        public ByteBufferPool(int poolSize, int maxBufferSize)
        {
            _poolSize = poolSize;
            _pool = new PooledByteBuffer[poolSize];
            
            for (int idx = 0; idx < poolSize; ++idx) {
                _pool[idx] = new PooledByteBuffer(idx, maxBufferSize);
            }
        }

        public int Available()
        {
            // this is not an accurate calculation because the number
            // of in-use buffers could change before the loop completes
            // and/or the result is answered.
            var available = _poolSize;
    
            for (int idx = 0; idx < _poolSize; ++idx) {
                if (_pool[idx].IsInUse()) {
                    --available;
                }
            }
    
            return available;
        }
        
        public PooledByteBuffer Access() => AccessFor("untagged");

        public PooledByteBuffer AccessFor(string tag) => AccessFor(tag, int.MaxValue);

        public PooledByteBuffer AccessFor(string tag, int retries)
        {
            while (true) {
                for (int idx = 0; idx < _poolSize; ++idx) {
                    var buffer = _pool[idx];
                    if (buffer.ClaimUse(tag)) {
                        return buffer;
                    }
                }
            }
        }
        
        public class PooledByteBuffer : BasicConsumerByteBuffer
        {
            private readonly AtomicBoolean _inUse;

            public PooledByteBuffer(int id, int maxBufferSize) : base(id, maxBufferSize)
            {
                _inUse = new AtomicBoolean(false);
            }

            public void Release()
            {
                if (!_inUse.Get())
                {
                    throw new InvalidOperationException($"Attempt to release unclaimed buffer: {this}");
                }
                
                NotInUse();
            }
            
            public bool IsInUse() => _inUse.Get();
            
            public bool ClaimUse(string tag)
            {
                if (_inUse.CompareAndSet(false, true))
                {
                    Tag = tag;
                    AsStream().SetLength(0);
                    return true;
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(BasicConsumerByteBuffer))
                {
                    return false;
                }

                return Id == ((BasicConsumerByteBuffer) obj).Id;
            }

            public override int GetHashCode() => 31 * Id.GetHashCode();

            public override string ToString() => $"PooledByteBuffer[id={Id}]";

            private void NotInUse() => _inUse.Set(false);
        }
    }
}