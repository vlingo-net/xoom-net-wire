// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using Vlingo.Xoom.Common;
using Vlingo.Xoom.Wire.Message;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 618

namespace Vlingo.Xoom.Wire.Tests.Message;

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
    [Fact]
    public void TestAlwaysAccessible()
    {
        var pool = new ByteBufferPool(1, 100);
        var count = new AtomicInteger(0);

        var work1 = new Thread(() =>
        {
            for (int c = 0; c < 10_000_000; ++c)
            {
                var pooled = pool.Access();
                pooled.Clear().Put(Converters.TextToBytes("I got it: 1!")).Flip();
                pooled.Release();
            }

            count.IncrementAndGet();
        });
            
        var work2 = new Thread(() =>
        {
            for (int c = 0; c < 10_000_000; ++c)
            {
                var pooled = pool.Access();
                pooled.Clear().Put(Converters.TextToBytes("I got it: 2!")).Flip();
                pooled.Release();
            }

            count.IncrementAndGet();
        });

        work1.Start();
        work2.Start();

        work1.Join();
        work2.Join();

        Assert.Equal(2, count.Get());
    }

    public ByteBufferPoolTest(ITestOutputHelper output)
    {
        var converter = new Converter(output);
        Console.SetOut(converter);
    }
}