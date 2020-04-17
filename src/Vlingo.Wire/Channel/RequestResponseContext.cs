// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public abstract class RequestResponseContext
    {
        public abstract TR ConsumerData<TR>();
        public abstract TR ConsumerData<TR>(TR data);
        public abstract bool HasConsumerData { get; }
        public abstract string Id { get; }
        public abstract IResponseSenderChannel Sender { get; }
        public abstract void WhenClosing(object data);

        public void Abandon() => Sender.Abandon(this);

        public void ExplicitClose(bool option) => Sender.ExplicitClose(this, option);

        public void RespondWith(IConsumerByteBuffer buffer) => Sender.RespondWith(this, buffer);
    }
}