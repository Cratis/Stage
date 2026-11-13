// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_availability : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _registration = string.Empty;
    string _props = string.Empty;

    void Because()
    {
        _plan = a_fleet_model.Plan(an_available_fleet_model.Source);
        if (!_plan.Success) return;
        _registration = a_fleet_model.Text(_plan, "Availability.cs")
            .Replace("global::Cratis.Choreography.Availability.Audiences.", string.Empty, StringComparison.Ordinal)
            .Replace("global::Cratis.Choreography.Availability.Context.", string.Empty, StringComparison.Ordinal)
            .Replace("global::Cratis.Choreography.Availability.", string.Empty, StringComparison.Ordinal);
        _props = a_fleet_model.Text(_plan, "Availability.props");
    }

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_declare_the_audiences() => _registration.ShouldContain("new DeclaredAudiences(\"Beta\", \"Plus\")");
    [Fact] void should_define_the_reusable_availability() => _registration.ShouldContain("AvailabilityDefinition.FromLines(\"DataQuality\", AvailabilityExpression.For(\"Plus\"), AvailabilityExpression.When(new ContextComparison(\"region\", ComparisonOperator.Equal, (object)\"Nordics\")))");
    [Fact] void should_gate_the_feature_with_the_availability() => _registration.ShouldContain("options.GateNamespace(\"Fleet.Fleet.Vessels\", AvailabilityExpression.With(\"DataQuality\"))");
    [Fact] void should_gate_the_slice_for_its_audience() => _registration.ShouldContain("options.GateNamespace(\"Fleet.Fleet.Vessels.RegisterVessel\", AvailabilityExpression.For(\"Beta\"))");
    [Fact] void should_gate_the_command_for_either_audience() => _registration.ShouldContain("GateType(options, \"Fleet.Fleet.Vessels.RegisterVessel.RegisterVessel\", AvailabilityExpression.AnyOf(AvailabilityExpression.For(\"Plus\"), AvailabilityExpression.For(\"Beta\")))");
    [Fact] void should_gate_the_read_model_of_the_query_with_its_condition() => _registration.ShouldContain("GateType(options, \"Fleet.Fleet.Vessels.Vessels.Vessel\", AvailabilityExpression.AllOf(");
    [Fact] void should_compare_a_number_by_value() => _registration.ShouldContain("ComparisonOperator.GreaterThanOrEqual, (object)3m");
    [Fact] void should_scope_the_values_by_slice() => _registration.ShouldContain("[\"Fleet.Fleet.Vessels.Vessels\"] = ContextScope.Of(\"Fleet\", \"Vessels\", \"Vessels\")");
    [Fact] void should_share_the_provided_value_catalog() => _registration.ShouldContain("ProvidedValueResolvers.CreateCatalog(provider)");
    [Fact] void should_name_the_product_after_the_application() => _registration.ShouldContain("guestList.Product = \"fleet\"");
    [Fact] void should_feed_the_engine_from_the_guest_list() => _registration.ShouldContain("AddGuestListAvailability(services, engine)");
    [Fact] void should_hook_the_gates_into_arc() => _registration.ShouldContain("AddArcAvailability(services, options =>");
    [Fact] void should_reference_the_packages_from_a_generated_props_file() => _props.ShouldContain("Cratis.Showrunner.GuestList.Availability");
}
#endif
