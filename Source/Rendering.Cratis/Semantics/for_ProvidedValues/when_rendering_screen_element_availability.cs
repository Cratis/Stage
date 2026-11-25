// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_screen_element_availability : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _registration = string.Empty;
    string _route = string.Empty;
    IReadOnlyDictionary<string, string> _elements = null!;

    void Because()
    {
        var compilation = an_available_screen_element_model.Compile();
        _plan = a_fleet_model.Plan(compilation);
        _elements = ScreenAvailabilityParts.ElementKeys(compilation.Model.Application);
        if (!_plan.Success) return;
        _registration = a_fleet_model.Text(_plan, "Availability.cs");
        _route = a_fleet_model.Text(_plan, "stage.ts");
    }

    string ActionKey => _elements["VesselList/action/RegisterVessel"];

    string DataKey => _elements["VesselList/data/VesselByKey"];

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_map_a_gated_action_by_screen_and_command() => _elements.ContainsKey("VesselList/action/RegisterVessel").ShouldBeTrue();
    [Fact] void should_map_a_gated_data_element_by_screen_and_query() => _elements.ContainsKey("VesselList/data/VesselByKey").ShouldBeTrue();
    [Fact] void should_give_each_element_its_own_key() => _elements.Values.Distinct().Count().ShouldEqual(2);
    [Fact] void should_gate_the_action_marker_with_the_negated_audience() => _registration.ShouldContain($"GateType(options, \"{ActionKey}\"");
    [Fact] void should_render_the_negation_as_a_not_audience() => _registration.ShouldContain("new global::Cratis.Arc.Availability.AvailableNotAudience(\"Beta\")");
    [Fact] void should_keep_the_gates_above_the_element() => _registration.ShouldContain($"GateType(options, \"{DataKey}\", global::Cratis.Arc.Availability.AvailabilityExpression.AllOf(");
    [Fact] void should_use_the_declaring_slice_scope() => _registration.ShouldContain($"[\"{ActionKey}\"] = global::Cratis.Arc.Availability.Context.ContextScope.Of(\"Fleet\", \"Vessels\", \"Vessels\")");
    [Fact] void should_publish_an_element_target_with_no_backend_slices() => _registration.ShouldContain("element:Fleet/Vessels/Vessels/VesselList/action/RegisterVessel;slices=0");
    [Fact] void should_publish_the_element_keys_in_the_static_routes() => _route.ShouldContain($"\"VesselList/action/RegisterVessel\":\"{ActionKey}\"");
}
#endif
