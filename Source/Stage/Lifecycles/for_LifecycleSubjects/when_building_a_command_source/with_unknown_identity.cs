// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_unknown_identity : given.a_subject
{
    [Fact]
    void should_reject_an_unresolved_snapshot_property() =>
        Assert.Throws<InvalidOperationException>(() => LifecycleSubjects.CommandSource(_plan, _command, [new(Id(106), SemanticValue.Text("unknown"))]));
}
