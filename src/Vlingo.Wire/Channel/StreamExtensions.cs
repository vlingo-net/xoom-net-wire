// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.IO;

namespace Vlingo.Wire.Channel
{
    public static class StreamExtensions
    {
        public static void Flip(this Stream buffer)
        {
            buffer.SetLength(buffer.Position);
            buffer.Position = 0;
        }

        public static bool HasRemaining(this Stream buffer) => buffer.Length - buffer.Position > 0;

        public static void Clear(this MemoryStream buffer)
        {
            buffer.Position = 0;
            buffer.SetLength(buffer.Capacity);
        }
    }
}