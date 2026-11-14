// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_lossless_stored_utc_seed_date_time : a_stored_typed_event
{
    const string Value = "2026-09-24T12:00:00.1234567Z";
    SemanticWorld _world = null!;

    void Establish()
    {
        Prepare("DateTime", $"\"{Value}\"", exactSchema: true);
        _stored.Context.CausedBy = new() { Subject = SemanticChronicleRegistration.SeedSubject };
    }

    void Because() => _world = SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: Evidence);

    [Fact] void should_restore_the_utc_seed_exactly() => _world.Facts.Single().Values.Select(value => value.Value).ShouldContain(SemanticValue.Text(Value));
}
