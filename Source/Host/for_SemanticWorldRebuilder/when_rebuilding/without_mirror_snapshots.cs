// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class without_mirror_snapshots : a_rebuildable_world
{
    SemanticWorld _world = null!;

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, [_event], 0);

    [Fact] void should_rebuild_from_the_log_without_mirror_rows() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_preserve_the_stored_fact() => _world.Facts.Length.ShouldEqual(1);
}
