// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_selecting_refusal_modes : for_CratisArtifactRenderPlanner.when_rendering_around_refusals.given.a_refused_specification
{
    CratisPlanResult _result = null!;
    CratisPlanOptions _optionsForPlan = null!;

    void Establish() => _optionsForPlan = new(_original.Model.Application.Name, _options.ProjectName, _options.RootNamespace);
    void Because() => _result = CratisRendering.PlanFrom(new LoadedSemanticModel(_original.Model, _original.ExecutionPlan),
        new([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]),
        _optionsForPlan with { Refusals = ArtifactRefusalMode.RenderAround });

    [Fact] void should_default_to_refuse() => _optionsForPlan.Refusals.ShouldEqual(ArtifactRefusalMode.Refuse);
    [Fact] void should_carry_the_opt_in_through_source_planning() => _result.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_keep_success_false() => _result.Success.ShouldBeFalse();
    [Fact] void should_expose_the_refused_specification() => _result.Refusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_preserve_the_default_profile_roster() => CratisRendering.CreateProfile(_original.Model.Application.Name, _options).Inputs.Any(input => input.Name == RefusalModeInput.Name).ShouldBeFalse();
}
