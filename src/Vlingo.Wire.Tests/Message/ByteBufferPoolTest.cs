// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using Vlingo.Common;
using Vlingo.Wire.Message;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Wire.Tests.Message
{
    public class ByteBufferPoolTest
    {
        [Fact]
        public void TestAccessAvailableRelease()
        {
            var pool = new ByteBufferPool(10, 100);
            
            Assert.Equal(10, pool.Available());

            var buffer1 = pool.Access();
            var buffer2 = pool.Access();
            var buffer3 = pool.Access();
            
            Assert.Equal(7, pool.Available());
            
            buffer1.Release();
            
            Assert.Equal(8, pool.Available());
            
            buffer2.Release();
            buffer3.Release();
            
            Assert.Equal(10, pool.Available());
        }

        [Fact]
        public void TestPooledByteBuffer()
        {   
            var testText = "Hello, PooledByteBuffer";
            
            var pool = new ByteBufferPool(10, 100);
            
            var buffer1 = pool.Access();
            
            buffer1.AsStream().Write(Converters.TextToBytes(testText));
            
            Assert.Equal(testText, Converters.BytesToText(buffer1.Flip().ToArray()));
        }

        // TODO : This yields poor performance compared to java. Has to be investigated.
        [Fact(Timeout = 6 * 1000)]
        public async Task TestAlwaysAccessible()
        {
            var pool = new ByteBufferPool(1, 100);
            var count = new AtomicInteger(0);

            var t1 = Task.Factory.StartNew(() =>
            {
                for (int c = 0; c < 10_000_000; ++c)
                {
                    var pooled = pool.Access();
                    pooled.Clear().Put(Converters.TextToBytes("I got it: 1!")).Flip();
                    pooled.Release();
                }

                count.IncrementAndGet();
            }, TaskCreationOptions.LongRunning);

            var t2 = Task.Factory.StartNew(() =>
            {
                for (int c = 0; c < 10_000_000; ++c)
                {
                    var pooled = pool.Access();
                    pooled.Clear().Put(Converters.TextToBytes("I got it: 2!")).Flip();
                    pooled.Release();
                }

                count.IncrementAndGet();
            }, TaskCreationOptions.LongRunning);

            await Task.WhenAll(t1, t2);

            Assert.Equal(2, count.Get());
        }

        public ByteBufferPoolTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
        }
    }
}