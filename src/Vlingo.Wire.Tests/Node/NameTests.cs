//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Node;
using Xunit;

namespace Vlingo.Wire.Tests.Node
{
    public class NameTests
    {
        [Fact]
        public void TestNameCreationState()
        {
            var name1 = new Name("name1");
            var name2 = new Name("name2");
            Assert.NotEqual(name2, name1);
            Assert.False(name1.HasNoName);
            Assert.True(name1.SameAs("name1"));
            Assert.Equal("name2", name2.Value);
        }
    }
} 