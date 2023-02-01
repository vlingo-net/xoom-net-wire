// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Actors.TestKit;
using Vlingo.Xoom.Wire.Tests.Nodes;

namespace Vlingo.Xoom.Wire.Tests.Message;

public class AbstractMessageTool
{
    protected MockConfiguration Config = new MockConfiguration();
    protected TestWorld TestWorld = TestWorld.Start("xoom-wire-test");
}