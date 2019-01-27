// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

namespace Vlingo.Wire.Message
{
    public class RawMessageHeader
    {
        private static readonly int ShortFields = 5;
        private static readonly int IntFields = 1;
        private static readonly int ShortBytes = sizeof(short) / sizeof(byte);
        private static readonly int IntBytes = sizeof(int) / sizeof(byte);
        private static readonly short HeaderId = 3730 | 0x01; // version 1

        public static int Bytes { get; } = ShortBytes * ShortFields + IntBytes * IntFields;
    }
}