//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Wire.Node;
using Xunit;

namespace Vlingo.Wire.Tests.Node
{
    public class IdTest
    {
        [Fact]
        public void TestIdCreationState()
        {
            var id = Id.Of(1);
            Assert.Equal(id, Id.Of(1));
            Assert.Equal(1, id.Value);
            Assert.False(id.HasNoId);
            Assert.True(id.IsValid);
        }
        
        [Fact]
        public void TestIdComparisons()
        {
            var id1 = Id.Of(1);
            var id2 = Id.Of(2);
            Assert.NotEqual(0, id1.CompareTo(id2));
            Assert.Equal(-1, id1.CompareTo(id2));
//            Assert.True(id2.GreatherThan(id1));
//            Assert.False(id1.GreatherThan(id2));
        }
    }
}