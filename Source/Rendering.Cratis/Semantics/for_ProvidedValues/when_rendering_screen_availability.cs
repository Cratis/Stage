// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_screen_availability : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _parts = string.Empty;
    string _registration = string.Empty;
    IReadOnlyDictionary<string, string> _map = null!;

    void Because()
    {
        var compilation = a_fleet_model.Compile(an_available_screen_model.Source);
        _plan = a_fleet_model.Plan(compilation);
        _map = ScreenAvailabilityParts.For(compilation.Model.Application);
        if (!_plan.Success) return;
        _parts = a_fleet_model.Text(_plan, "ScreenParts.cs");
        _registration = a_fleet_model.Text(_plan, "Availability.cs");
    }

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_map_a_direct_gate() => _map.ContainsKey("VesselList").ShouldBeTrue();
    [Fact] void should_map_an_inherited_gate() => _map.ContainsKey("InheritedList").ShouldBeTrue();
    [Fact] void should_not_gate_a_screen_without_a_direct_or_inherited_gate() => _map.ContainsKey("PublicList").ShouldBeFalse();
    [Fact] void should_register_an_exact_type_gate() => _registration.ShouldContain($"GateType(options, \"{_map["VesselList"]}\"");
    [Fact] void should_combine_inherited_and_direct_requirements() => _registration.ShouldContain($"GateType(options, \"{_map["VesselList"]}\", global::Cratis.Choreography.Availability.AvailabilityExpression.AllOf(");
    [Fact] void should_use_the_declaring_screen_scope() => _registration.ShouldContain($"[\"{_map["VesselList"]}\"] = global::Cratis.Choreography.Availability.Context.ContextScope.Of(\"Fleet\", \"Vessels\", \"Vessels\")");
    [Fact] void should_emit_the_same_keys_in_the_static_route_map() => _map.Values.All(key => a_fleet_model.Text(_plan, "stage.ts").Contains(key, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_publish_a_screen_target_with_no_backend_slices() => _registration.ShouldContain("screen:Fleet/Vessels/Vessels/VesselList;slices=0");
    [Fact] void should_emit_distinct_stable_keys() => _map.Values.Distinct().Count().ShouldEqual(2);
    [Fact] void should_not_assign_a_namespace_to_marker_parts() => _parts.Contains("namespace ", StringComparison.Ordinal).ShouldBeFalse();
}
#endif
