// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Node;

namespace Vlingo.Xoom.Wire.Tests.Node
{
    public class MockConfiguration : IConfiguration
    {
        private readonly ISet<Xoom.Wire.Node.Node> _nodes;

        public MockConfiguration()
        {
            var node1 = Xoom.Wire.Node.Node.With(Id.Of(1), Name.Of("node1"), Host.Of("localhost"), 37371, 37372);
            var node2 = Xoom.Wire.Node.Node.With(Id.Of(2), Name.Of("node2"), Host.Of("localhost"), 37373, 37374);
            var node3 = Xoom.Wire.Node.Node.With(Id.Of(3), Name.Of("node3"), Host.Of("localhost"), 37375, 37376);
            
            _nodes = new SortedSet<Xoom.Wire.Node.Node>(new [] {node1, node2, node3});
        }
        
        public IEnumerable<Xoom.Wire.Node.Node> AllNodesOf(IEnumerable<Id> ids) => new List<Xoom.Wire.Node.Node>();

        public IEnumerable<Xoom.Wire.Node.Node> AllGreaterNodes(Id id) => _nodes.Where(node => node.Id.GreaterThan(id));

        public IEnumerable<Xoom.Wire.Node.Node> AllOtherNodes(Id id) => _nodes.Where(node => !node.Id.Equals(id));

        public IEnumerable<Id> AllOtherNodesId(Id id) => AllOtherNodes(id).Select(node => node.Id);

        public Xoom.Wire.Node.Node NodeMatching(Id id)
        {
            var firstNode = _nodes.FirstOrDefault(node => node.Id.Equals(id));
            if (firstNode != null)
            {
                return firstNode;
            }
            return Xoom.Wire.Node.Node.NoNode;
        }

        public bool HasNode(Id id) => _nodes.Any(node => node.Id.Equals(id));

        public IEnumerable<Xoom.Wire.Node.Node> AllNodes => _nodes;

        public IEnumerable<string> AllNodeNames => _nodes.Select(node => node.Name.Value);

        public Id GreatestNodeId => _nodes.Max(node => node.Id);

        public int TotalNodes => _nodes.Count;

        public ILogger Logger => null;
    }
}