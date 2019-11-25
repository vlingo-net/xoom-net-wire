// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using Vlingo.Wire.Fdx.Outbound;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Tests.Fdx.Outbound
{
    public class MockManagedOutboundChannel : IManagedOutboundChannel
    {
        public MockManagedOutboundChannel(Id id)
        {
            Id = id;
        }
        
        public Id Id { get; }
        
        public List<string> Writes { get; } = new List<string>();
        
        public void Close()
        {
            Writes.Clear();
        }

        public void Write(Stream buffer)
        {
            var message = RawMessage.ReadFromWithHeader(buffer);
            Writes.Add(message.AsTextMessage());
        }
    }
}