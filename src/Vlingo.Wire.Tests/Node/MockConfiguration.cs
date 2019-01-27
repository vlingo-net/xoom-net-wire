// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Actors;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Tests.Node
{
    public class MockConfiguration : IConfiguration
    {
        public IEnumerable<Wire.Node.Node> AllNodesOf(IEnumerable<Id> ids)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Wire.Node.Node> AllGreaterNodes(Id id)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Wire.Node.Node> AllOtherNodes(Id id)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Id> AllOtherNodesId(Id id)
        {
            throw new System.NotImplementedException();
        }

        public Wire.Node.Node NodeMatching(Id id)
        {
            throw new System.NotImplementedException();
        }

        public bool HasNode(Id id)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Wire.Node.Node> AllNodes { get; }
        public IEnumerable<string> AllNodeNames { get; }
        public Id GreatestNodeId { get; }
        public int TotalNodes { get; }
        public ILogger Logger { get; }
    }
}