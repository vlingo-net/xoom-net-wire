// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Channel;
using Xunit;

namespace Vlingo.Wire.Tests.Channel
{
    public class NetExtensionsTest
    {
        [Fact]
        public void TestThatIsLocalIpAddress()
        {
            Assert.True("localhost".IsLocalIpAddress());
        }
    }
}