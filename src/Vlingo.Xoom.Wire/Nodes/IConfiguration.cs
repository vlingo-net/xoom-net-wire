// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Xoom.Actors;

namespace Vlingo.Xoom.Wire.Nodes
{
    public interface IConfiguration
    {   
        IEnumerable<Node> AllNodesOf(IEnumerable<Id> ids);

        IEnumerable<Node> AllGreaterNodes(Id id);

        IEnumerable<Node> AllOtherNodes(Id id);

        IEnumerable<Id> AllOtherNodesId(Id id);

        Node NodeMatching(Id id);

        bool HasNode(Id id);
        
        IEnumerable<Node> AllNodes { get; }
        
        IEnumerable<string> AllNodeNames { get; }
        
        Id GreatestNodeId { get; }
        
        int TotalNodes { get; }
        
        ILogger Logger { get; }
    }
}