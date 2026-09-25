// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_stored_date_time : a_stored_typed_event
{
    Exception? _error;

    void Establish() => Prepare("DateTime", "\"2026-09-24T12:00:00.1234567+02:00\"");
    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Fact(_typedPlan, _stored));

    [Fact] void should_convert_through_chronicles_date_time_schema() => _converted.ShouldBeOfExactType<DateTime>();
    [Fact] void should_refuse_the_non_exact_event_property() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_refused_property() => _error!.Message.ShouldContain("value");
}
