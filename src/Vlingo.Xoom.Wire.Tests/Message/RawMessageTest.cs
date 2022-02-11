// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;
using Xunit;

namespace Vlingo.Xoom.Wire.Tests.Message;

public class RawMessageTest
{
    [Fact]
    public void TestCopyBytesTo()
    {
        var buffer = new MemoryStream(1000);
        var text = "JOIN\nid=1 nm=node1 op=localhost:35745 app=localhost:35746";
        var node1 = Node.With(Id.Of(1), Name.Of("node1"), Host.Of("localhost"), 35745, 35746);
        var bytes = Converters.TextToBytes(text);
        buffer.Write(bytes, 0, bytes.Length);
        buffer.Flip();
        var messageSize = buffer.Length;
        var messageBytes = new byte[messageSize];
        Array.Copy(buffer.ToArray(), 0, messageBytes, 0, messageSize);
        var message = new RawMessage(messageBytes);
        message.Header(RawMessageHeader.From(node1.Id.Value, 0, message.Length));
        Assert.Equal(text, message.AsTextMessage());
            
        buffer.Clear();
        message.CopyBytesTo(buffer); // copyBytesTo
        var convertedText = buffer.ToArray().BytesToText(RawMessageHeader.Bytes, (int)message.Length);
        Assert.Equal(text, convertedText);
    }
}