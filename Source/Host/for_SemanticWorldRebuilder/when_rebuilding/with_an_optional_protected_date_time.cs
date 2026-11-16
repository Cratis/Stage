// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_optional_protected_date_time : a_stored_typed_event
{
    const string Value = "2026-09-24T12:00:00.1234567+05:30";

    [Theory]
    [InlineData("pii")]
    [InlineData("secret")]
    public void should_accept_lossless_nullable_date_time_storage(string protection)
    {
        Prepare("Protected?", $"\"{Value}\"", exactSchema: true, concepts: $"concept Protected : DateTime {protection}");

        var world = SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: Evidence);

        world.Facts.Single().Values.Select(value => value.Value).ShouldContain(SemanticValue.Text(Value));
        _converted.ShouldBeOfExactType<string>();
    }

    [Fact]
    public void should_refuse_a_legacy_nullable_pii_date_time_with_the_diagnostic()
    {
        Prepare("Protected?", $"\"{Value}\"", concepts: "concept Protected : DateTime pii");

        var error = Catch.Exception(() => SemanticWorldRebuilder.Create(_typedPlan, [_stored], 0, evidence: Evidence));

        error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
        error.Message.ShouldContain("ProjectRegistered");
        error.Message.ShouldContain("'value'");
        error.Message.ShouldContain("offset was not stored");
        error.Message.ShouldContain("Start a new history");
    }
}
