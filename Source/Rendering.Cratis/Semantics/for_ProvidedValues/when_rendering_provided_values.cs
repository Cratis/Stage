// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_provided_values : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _resolvers = string.Empty;

    void Because()
    {
        _plan = a_fleet_model.Plan(a_fleet_model.Source);
        _resolvers = _plan.Success ? a_fleet_model.Text(_plan, "ProvidedValues.cs") : string.Empty;
    }

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_register_the_scoped_resolver() => _resolvers.ShouldContain("AddScoped<ProvidedValueResolvers>(services)");
    [Fact] void should_register_the_host_values_the_application_can_replace() => _resolvers.ShouldContain("AddSingleton<IHostContextValues, InMemoryHostContextValues>(services)");
    [Fact] void should_declare_the_literal_at_the_module() => _resolvers.ShouldContain("new ContextValueDeclaration(\"region\", new LiteralContextValueProvider((string)\"Nordics\"), ContextScope.Of(\"Fleet\"), true)");
    [Fact] void should_declare_the_optional_host_value_at_the_module() => _resolvers.ShouldContain("new ContextValueDeclaration(\"operator\", new HostContextValueProvider(");
    [Fact] void should_declare_the_vessel_at_its_slice() => _resolvers.ShouldContain("ContextScope.Of(\"Fleet\", \"Vessels\", \"Vessels\"), false)");
    [Fact] void should_read_the_vessel_through_its_query_keyed_by_the_argument() => _resolvers.ShouldContain("new ReadModelContextValueProvider(new ProvidedReadModelQuery(services), \"VesselByKey\", \"arguments.vesselId\")");
    [Fact] void should_call_the_generated_query() => _resolvers.ShouldContain("return await Vessel.VesselByKey(readModels, id)");
    [Fact] void should_carry_the_file_in_the_plan_so_a_later_render_without_it_removes_it() => _plan.Artifacts.Count(artifact => artifact.RelativePath.EndsWith("GeneratedProvides/ProvidedValues.cs", StringComparison.Ordinal)).ShouldEqual(1);
}
#endif
