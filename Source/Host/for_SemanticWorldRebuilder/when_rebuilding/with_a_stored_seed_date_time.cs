// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_stored_seed_date_time : a_stored_typed_event
{
    Exception? _error;

    void Establish()
    {
        Prepare("DateTime", "\"2026-09-24T12:00:00.1234567+02:00\"");
        _stored.Context.CausedBy = new() { Subject = SemanticChronicleRegistration.SeedSubject };
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Fact(_typedPlan, _stored, evidence: Evidence));

    [Fact] void should_refuse_the_lossy_stored_seed() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_event_type() => _error.Message.ShouldContain("ProjectRegistered");
    [Fact] void should_name_the_refused_property() => _error.Message.ShouldContain("value");
    [Fact] void should_require_a_new_history() => _error.Message.ShouldContain("Start a new history");
}
