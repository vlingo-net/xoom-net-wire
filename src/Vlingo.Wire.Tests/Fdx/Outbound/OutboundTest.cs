// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Tests.Message;

namespace Vlingo.Wire.Tests.Fdx.Outbound
{
    public class OutboundTest : AbstractMessageTool
    {
        private static readonly string Message1 = "Message1";
        private static readonly string Message2 = "Message2";
        private static readonly string Message3 = "Message3";

        private MockManagedOutboundChannelProvider _provider;
    }
}