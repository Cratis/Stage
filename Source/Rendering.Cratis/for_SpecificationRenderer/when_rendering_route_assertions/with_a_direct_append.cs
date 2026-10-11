// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_a_direct_append : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var model = routed_specifications.Compile();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        var expected = specification.ThenEvents[0];
        var changed = specification with
        {
            When = null,
            WhenAppended = new(expected.EventContract, expected.Values) { EventSource = new(slice.Commands[0].Properties[0].Type, SemanticValue.Text("acc-1")), Route = expected.Route }
        };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [changed] }] }] }] };
        _plan = routed_specifications.Plan(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
    }

    // Direct-append specs have never been rendered. Keep that refusal rather than treating the action fact
    // as the following fact a v6+ then expectation compares against.
    [Fact] void should_keep_the_direct_append_outside_the_rendered_subset() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_not_generate_an_incorrect_action_fact_assertion() => _plan.Artifacts.ShouldBeEmpty();
}
