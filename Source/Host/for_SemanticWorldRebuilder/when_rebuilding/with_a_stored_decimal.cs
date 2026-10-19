// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_stored_decimal : a_stored_typed_event
{
    Exception? _error;

    void Establish()
    {
        Prepare("Decimal", "12345678901234567.89");
        _stored.Context.CausedBy = new() { Subject = "caller", Name = SemanticChronicleRegistration.SeedSubject, UserName = SemanticChronicleRegistration.SeedSubject };
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0));

    [Fact] void should_convert_through_chronicles_number_schema() => _converted.ShouldBeOfExactType<double>();
    [Fact] void should_refuse_the_non_exact_event_property() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_refused_property() => _error!.Message.ShouldContain("value");
}
