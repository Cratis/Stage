// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_the_availability_manifest : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _manifest = string.Empty;
    string _registration = string.Empty;

    void Because()
    {
        _plan = a_fleet_model.Plan(an_available_fleet_model.Source);
        if (!_plan.Success) return;
        _registration = a_fleet_model.Text(_plan, "Availability.cs");
        _manifest = availability_manifest_text.From(_registration);
    }

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_register_the_manifest_as_a_singleton() => _registration.ShouldContain("AddSingleton<global::Cratis.Choreography.Availability.IAvailabilityManifest>(services, new global::Cratis.Choreography.Availability.InMemoryAvailabilityManifest(manifest))");
    [Fact] void should_name_the_origin_after_the_application() => _manifest.ShouldContain("AvailabilityManifest(\n            \"Fleet\",");
    [Fact] void should_list_the_audiences_in_name_order() => Assert.True(_manifest.IndexOf("AudienceDeclaration(\"Beta\")", StringComparison.Ordinal) < _manifest.IndexOf("AudienceDeclaration(\"Plus\")", StringComparison.Ordinal));
    [Fact] void should_describe_the_reusable_availability_from_the_catalog() => _manifest.ShouldContain("AvailabilityBlockEntry.From(catalog.Get(\"DataQuality\"), catalog)");
    [Fact] void should_describe_the_feature_gate_with_the_slices_beneath_it() => _manifest.ShouldContain("AvailabilityGateEntry.From(\"feature:Fleet/Vessels;slices=2\"");
    [Fact] void should_describe_the_slice_gate() => _manifest.ShouldContain("AvailabilityGateEntry.From(\"slice:Fleet/Vessels/RegisterVessel;slices=1\", global::Cratis.Choreography.Availability.AvailabilityExpression.For(\"Beta\"), catalog)");
    [Fact] void should_describe_the_command_gate() => _manifest.ShouldContain("AvailabilityGateEntry.From(\"command:Fleet/Vessels/RegisterVessel/RegisterVessel;slices=1\"");
    [Fact] void should_describe_the_query_gate() => _manifest.ShouldContain("AvailabilityGateEntry.From(\"query:Fleet/Vessels/Vessels/VesselByKey;slices=1\"");
    [Fact] void should_describe_the_provided_values_with_their_scope() => _manifest.ShouldContain("ContextValueEntry(\"limit\", \"WholeNumber\", \"provided from a value\", global::Cratis.Choreography.Availability.Context.ContextScope.Of(\"Fleet\"), true)");
    [Fact] void should_carry_no_personas() => _manifest.ShouldContain("    []);");
}
#endif
