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

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Fact(_typedPlan, _stored));

    [Fact] void should_accept_the_stored_seed_event() => _error.ShouldBeNull();
}
