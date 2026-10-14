// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_screen_fragments;

public class with_inherited_contributions : given.a_partitioned_screen_plan
{
    protected override string ModelSource => Source.Replace("module Projects\n", "module Projects\n  contribute to Navigation\n    label \"Shared navigation\"\n    navigate to StaticOverview\n", StringComparison.Ordinal);

    void Because() => _application = AppPlan(_profile);

    [Fact] void should_admit_the_plan() => _application.Success.ShouldBeTrue();
    [Fact] void should_hide_inherited_contributions_from_fragments() => Fragments(_application.Artifacts).All(fragment => !Text(fragment).Contains("Shared navigation", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_inherited_contributions_in_the_app_scene() => Text(_application.Artifacts.Single(artifact => artifact.RelativePath == "scene.json")).ShouldContain("Shared navigation");
    [Fact] void should_reassemble_the_app_scene_byte_identically() => _application.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Sha256.ShouldEqual(AppPlan(_legacy).Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Sha256);
}
