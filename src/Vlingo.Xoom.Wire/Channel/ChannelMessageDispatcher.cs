// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Message;

namespace Vlingo.Xoom.Wire.Channel
{
    public abstract class ChannelMessageDispatcher
    {
        public abstract IChannelReaderConsumer? Consumer { get; }

        public abstract ILogger Logger { get; }

        public abstract string Name { get; }

        public virtual void DispatchMessageFor(RawMessageBuilder? builder)
        {
            if (builder == null || !builder.HasContent)
            {
                return;
            }

            builder.PrepareContent().Sync();

            while (builder.IsCurrentMessageComplete())
            {
                try
                {
                    var message = builder.CurrentRawMessage();
                    Logger.Debug($"ChannelMessageDispatcher received and dispatching message '{message.AsTextMessage()}'");
                    Consumer?.Consume(message);
                }
                catch (Exception e)
                {
                    Logger.Error($"Cannot dispatch message for: '{Name}'", e);
                }

                builder.PrepareForNextMessage();

                if (builder.HasContent)
                {
                    builder.Sync();
                }
            }
        }
    }
}
