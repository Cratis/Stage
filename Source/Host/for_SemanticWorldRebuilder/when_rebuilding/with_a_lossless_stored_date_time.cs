// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_lossless_stored_date_time : a_stored_typed_event
{
    const string Value = "2026-09-24T12:00:00.1234567+05:30";
    SemanticWorld _world = null!;

    void Establish() => Prepare("DateTime", $"\"{Value}\"", exactSchema: true);

    void Because() => _world = SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: Evidence);

    [Fact] void should_store_the_date_time_as_text() => _converted.ShouldBeOfExactType<string>();
    [Fact] void should_restore_the_date_time_exactly() => _world.Facts.Single().Values.Select(value => value.Value).ShouldContain(SemanticValue.Text(Value));
    [Fact] void should_preserve_the_offset_and_seven_fractional_digits() => JsonElement.Parse(_stored.Content).GetProperty("value").GetString().ShouldEqual(Value);
}
