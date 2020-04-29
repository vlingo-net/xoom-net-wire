// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using DotNetty.Transport.Channels;
using Vlingo.Common;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Bidirectional.Netty.Server
{
    public class NettyServerChannelContext : RequestResponseContext
    {
        private static readonly AtomicLong ContextId = new AtomicLong(0);
        private readonly IChannelHandlerContext _nettyChannelContext;
        private object? _closingData;
        private object? _consumerData;
        private readonly string _id;
        private readonly IResponseSenderChannel _sender;

        public NettyServerChannelContext(IChannelHandlerContext nettyChannelContext, IResponseSenderChannel sender)
        {
            _nettyChannelContext = nettyChannelContext;
            _sender = sender;
            _id = $"{ContextId.IncrementAndGet()}";
        }

        public override TR ConsumerData<TR>() => (TR) _consumerData!;
        public override TR ConsumerData<TR>(TR data)
        {
            _consumerData = data;
            return data;
        }

        public override bool HasConsumerData => _consumerData != null;
        public override string Id => _id;
        public override IResponseSenderChannel Sender => _sender;
        public override void WhenClosing(object data) => _closingData = data;

        public IChannelHandlerContext NettyChannelContext => _nettyChannelContext;
    }
}