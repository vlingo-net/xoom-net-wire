// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Wire.Message;

namespace Vlingo.Xoom.Wire.Channel;

public interface IResponseSenderChannel
{
    void Abandon(RequestResponseContext context);
    void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer);
    void RespondWith(RequestResponseContext context, IConsumerByteBuffer buffer, bool closeFollowing);
    void RespondWith(RequestResponseContext context, object response, bool closeFollowing);
}