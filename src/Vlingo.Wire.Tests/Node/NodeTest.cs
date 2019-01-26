//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Node;
using Xunit;

namespace Vlingo.Wire.Tests.Node
{
    public class NodeTest
    {
        [Fact]
        public void TestNodeCreationState()
        {
            var id1 = Id.Of(1);
            var name1 = new Name("name1");
        }
    }
}