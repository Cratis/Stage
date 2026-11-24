// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

// Generated Screenplay DateTime uses DateTimeOffset. This small consumed-package probe also guards CLR DateTime concepts.
public class with_both_clr_backing_types
{
    [Theory]
    [InlineData("2026-10-17T00:00:00.1234567Z")]
    [InlineData("2026-10-17T02:00:00.1234567+02:00")]
    void should_use_arcs_concept_converter_for_both_backing_types(string instant)
    {
        var command = JsonSerializer.Deserialize<clr_date_inputs.Command>($$"""{"at":"{{instant}}","offsetAt":"{{instant}}","optionalAt":"{{instant}}","optionalOffsetAt":"{{instant}}"}""", datetime_concept_model.Options);
        var expected = DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture);
        command.At.Value.ShouldEqual(expected.UtcDateTime);
        command.At.Value.Kind.ShouldEqual(DateTimeKind.Utc);
        command.OffsetAt.Value.EqualsExact(expected).ShouldBeTrue();
        command.OptionalAt.Value.ShouldEqual(command.At.Value);
        command.OptionalOffsetAt.Value.EqualsExact(command.OffsetAt.Value).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"optionalAt\":null,\"optionalOffsetAt\":null")]
    void should_keep_both_absent_clr_concepts_null(string assignment)
    {
        var command = JsonSerializer.Deserialize<clr_date_inputs.Command>("{\"at\":\"2026-10-17T00:00:00Z\",\"offsetAt\":\"2026-10-17T00:00:00Z\"" + assignment + "}", datetime_concept_model.Options);
        command.OptionalAt.ShouldBeNull();
        command.OptionalOffsetAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("at")]
    [InlineData("offsetAt")]
    [InlineData("optionalAt")]
    [InlineData("optionalOffsetAt")]
    void should_reject_offsetless_json_for_each_clr_concept(string property)
    {
        var refusal = Assert.Throws<ValueRequiresExplicitOffset>(() => JsonSerializer.Deserialize<clr_date_inputs.Command>($$"""{"{{property}}":"2026-10-17T00:00:00"}""", datetime_concept_model.Options));
        refusal.Path.ShouldEqual($"$.{property}");
        refusal.Message.ShouldEqual("The value requires an explicit offset or Z.");
    }
}
#endif
