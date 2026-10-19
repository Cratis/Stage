// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_integer_endpoint_fixtures : Specification
{
    readonly List<(string Code, string Identity)> _rendered = [];

    void Because()
    {
        foreach (var value in new[] { -9007199254740991L, 9007199254740991L })
        {
            Exercise(routed_specifications.Integer(value), value.ToString(CultureInfo.InvariantCulture));
            Exercise(routed_specifications.Composite(month: value), value.ToString(CultureInfo.InvariantCulture));
        }
    }

    void Exercise(ExecutableSemanticModel model, string identity)
    {
        var plan = routed_specifications.Plan(model);
        plan.Success.ShouldBeTrue();
        _rendered.Add((routed_specifications.Code(plan), identity));
        var module = model.Application.Modules[0];
        var feature = module.Features[0];
        var slice = feature.Slices[0];
        var specification = slice.Specifications[0];
        var expected = specification.ThenEvents[0];
        var append = specification with
        {
            When = null,
            WhenAppended = new(expected.EventContract, expected.Values)
            {
                EventSource = new(slice.Commands[0].Properties[0].Type, SemanticValue.Text("acc-1")),
                Route = expected.Route
            }
        };
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [append] }] }] }]
        });
        var appendPlan = routed_specifications.Plan(changed);

        // Direct-append specifications remain outside the rendered subset. Check the fixture codec
        // independently without widening that admission contract.
        appendPlan.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
        var options = new CratisRenderingOptions("Routed", "Routed");
        var request = new ArtifactRenderRequest(changed, SemanticExecutionPlan.Compile(changed).Plan!, CratisRendering.CreateProfile("Routed", options), new(ArtifactRenderScopeKind.Application, changed.Application.Id));
        _rendered.Add((SemanticSpecificationRouteRendering.Arguments(append.WhenAppended!.Route!, new(request, options)), identity));
    }

    [Fact] void should_preserve_exact_endpoint_bytes_in_given_append_and_then_routes() => _rendered.TrueForAll(rendered => rendered.Code.Contains($"\"{rendered.Identity}\"", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_use_an_unchecked_integer_helper() => _rendered.TrueForAll(rendered => !rendered.Code.Contains("StreamIds.Integer(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_render_all_scalar_and_composite_fixture_forms() => _rendered.Count.ShouldEqual(8);
}
