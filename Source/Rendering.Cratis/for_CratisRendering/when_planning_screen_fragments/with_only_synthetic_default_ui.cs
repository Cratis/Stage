// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_screen_fragments;

public class with_only_synthetic_default_ui : given.a_partitioned_screen_plan
{
    protected override string ModelSource => when_rendering_authoring_documentation.Source;

    void Because()
    {
        var profile = CratisRendering.WithScreenPartitions(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), _compilation);
        _application = AppPlan(profile);
        _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]), _options with { Profile = profile });
    }

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_default_ui_application_owned() => _application.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Sources.ShouldContainOnly(_loaded.Model.Application.Id);
    [Fact] void should_not_copy_the_synthetic_application_screen_to_the_slice() => Fragments(_result.Artifacts).ShouldBeEmpty();
    [Fact] void should_explain_why_the_slice_has_no_default_fragment() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-SCREEN-005").Artifact.ShouldEqual(Slice("RegisterProject").Id);
}
