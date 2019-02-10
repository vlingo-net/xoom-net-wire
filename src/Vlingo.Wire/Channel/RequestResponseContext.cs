// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Message;

namespace Vlingo.Wire.Channel
{
    public abstract class RequestResponseContext<T>
    {
        public abstract R ConsumerData<R>();
        public abstract R ConsumerData<R>(R data);
        public abstract bool HasConsumerData { get; }
        public abstract string Id { get; }
        public abstract IResponseSenderChannel<T> Sender { get; }
        public abstract void WhenClosing(object data);

        public void Abandon() => Sender.Abandon(this);

        public void RespondWith(IConsumerByteBuffer buffer) => Sender.RespondWith(this, buffer);
    }
}