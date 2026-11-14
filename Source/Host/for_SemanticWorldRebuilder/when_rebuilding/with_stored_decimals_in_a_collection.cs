// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_stored_decimals_in_a_collection : a_stored_typed_event
{
    SemanticWorld _world = null!;

    void Establish() => Prepare("Decimal[]", "[1.5, 12345678901234567.89]", exactSchema: true);

    void Because() => _world = SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: Evidence);

    [Fact] void should_restore_every_item_exactly() => ((SemanticArrayValue)_world.Facts.Single().Values.Single(value => value.Value is SemanticArrayValue).Value).Values.Select(item => ((SemanticNumberValue)item).Value).ShouldContainOnly(1.5m, 12345678901234567.89m);
}
