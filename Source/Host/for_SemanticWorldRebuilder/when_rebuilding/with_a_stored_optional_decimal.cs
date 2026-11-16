// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_stored_optional_decimal : a_stored_typed_event
{
    SemanticWorld _world = null!;

    void Establish() => Prepare("Decimal?", "12345678901234567.89", exactSchema: true);

    void Because() => _world = SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: Evidence);

    [Fact] void should_restore_the_decimal_exactly() => _world.Facts.Single().Values.Select(value => value.Value).ShouldContain(SemanticValue.Number(12345678901234567.89m));
}
