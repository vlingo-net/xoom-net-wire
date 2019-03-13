// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Buffers;
using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public interface IRequestChannelConsumer
    {
        void CloseWith<T>(RequestResponseContext<T> requestResponseContext, object data);
        void Consume<T>(RequestResponseContext<T> context, IConsumerByteBuffer buffer);
        
        // Experimental System.IO.Pipelines
        void Consume<T>(RequestResponseContext<T> context, ReadOnlySequence<byte> buffer);
    }
}