// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.IO;
using System.Text;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Message
{
    public static class Converters
    {
        public static Encoding EncodingValue { get; private set; } = Encoding.GetEncoding(Encoding.UTF8.WebName);

        public static string BytesToText(this byte[] bytes, int index, int length) =>
            EncodingValue.GetString(bytes, index, length);

        public static string BytesToText(byte[] bytes) => EncodingValue.GetString(bytes);

        public static void ChangeEncoding(string encodingName) => EncodingValue = Encoding.GetEncoding(encodingName);

        public static byte[] TextToBytes(string text) => EncodingValue.GetBytes(text);

        public static RawMessage ToRawMessage(this short sendingNodeId, Stream buffer)
        {
            buffer.Flip();
            var message = new RawMessage(buffer.Length);
            message.Put(buffer, false);
            buffer.SetLength(0); // clear
            
            var header = new RawMessageHeader(sendingNodeId, (short)0, (int) message.Length);
            message.Header(header);
            return message;
        }
    }
}