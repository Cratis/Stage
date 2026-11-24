// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

public class with_a_primitive_input : Specification
{
    Assembly _assembly;

    void Establish() => _assembly = datetime_concept_model.Load(datetime_concept_model.Source()
        .Replace("from Moment", "from DateTime", StringComparison.Ordinal)
        .Replace("to Moment", "to DateTime", StringComparison.Ordinal));

    [Fact] void should_keep_the_primitive_property_type() => datetime_concept_model.Command(_assembly).GetProperty("From").PropertyType.ShouldEqual(typeof(DateTimeOffset));

    [Fact]
    void should_keep_the_primitive_explicit_offset_binding()
    {
        var command = datetime_concept_model.Bind(_assembly, "{\"id\":\"one\",\"from\":\"2026-10-17T00:00:00Z\",\"to\":\"2026-10-17T02:00:00+02:00\"}");
        datetime_concept_model.Validate(_assembly, command).IsValid.ShouldBeTrue();
    }

    [Fact]
    void should_keep_rejecting_offsetless_primitive_json()
    {
        var refusal = Assert.Throws<ValueRequiresExplicitOffset>(() => datetime_concept_model.Bind(_assembly, "{\"id\":\"one\",\"from\":\"2026-10-17T00:00:00\",\"to\":\"2026-10-17T00:00:00Z\"}"));
        refusal.Path.ShouldEqual("$.from");
        refusal.Message.ShouldEqual("The value requires an explicit offset or Z.");
    }
}
#endif
