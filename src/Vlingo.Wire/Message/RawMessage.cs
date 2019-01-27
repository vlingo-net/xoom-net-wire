// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

namespace Vlingo.Wire.Message
{
    /**
    * Reusable raw message with header. Assume one instance per client channel.
    * Thus, the header and the bytes are reused to avoid ridicules GC.
    */
    public class RawMessage
    {
        private readonly byte[] _bytes;
        
    }
}