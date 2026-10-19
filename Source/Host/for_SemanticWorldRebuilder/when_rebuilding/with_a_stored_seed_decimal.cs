// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_stored_seed_decimal : a_stored_typed_event
{
    SemanticWorld _world = null!;

    void Establish()
    {
        Prepare("Decimal", "12345678901234567.89");
        _stored.Context.CausedBy = new() { Subject = SemanticChronicleRegistration.SeedSubject };
    }

    void Because() => _world = SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0);

    [Fact] void should_accept_the_stored_seed_event() => _world.Facts.Length.ShouldEqual(1);
    [Fact] void should_restore_the_stored_decimal_value() => _world.Facts.Single().Values.Select(value => value.Value).ShouldContain(SemanticValue.Number(JsonDocument.Parse(_stored.Content).RootElement.GetProperty("value").GetDecimal()));
}
