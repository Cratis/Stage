// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class without_a_pin_or_concept_rules : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _specification = null!;

    void Because()
    {
        _plan = opaque_policy_model.Plan(generation_rule_model.Load(pinned: false));
        _specification = string.Join('\n', _plan.Artifacts.Where(artifact => artifact.RelativePath.Contains("when_creating_an_order", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_allow_generation() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_supply_the_scenario_clock() => _specification.ShouldContain("_scenario.Clock(");
    [Fact] void should_compile_the_unpinned_specification() => RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
}
#endif
