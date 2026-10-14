// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_screen_fragments;

public class with_a_static_screen : given.a_partitioned_screen_plan
{
    void Because()
    {
        _application = AppPlan(_profile);
        _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]), _options);
    }

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_emit_only_the_declared_static_screen() => Text(Fragments(_result.Artifacts).Single()).ShouldContain("StaticOverview");
    [Fact] void should_not_emit_the_other_screen() => Text(Fragments(_result.Artifacts).Single()).ShouldNotContain("LookupOverview");
    [Fact] void should_attribute_the_fragment_only_to_its_declaring_slice() => Fragments(_result.Artifacts).Single().Sources.ShouldContainOnly(Slice("RegisterProject").Id);
    [Fact] void should_emit_the_business_file() => _result.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_match_the_application_fragment_bytes() => Fragments(_result.Artifacts).Single().Sha256.ShouldEqual(Fragments(_application.Artifacts).Single(artifact => artifact.Sources.Contains(Slice("RegisterProject").Id)).Sha256);
    [Fact] void should_omit_every_shared_scene_aggregate() => _result.Artifacts.Any(artifact => artifact.RelativePath == "scene.json" || artifact.RelativePath == "src/bindings.ts" || artifact.RelativePath == "src/stage-scene.json" || artifact.RelativePath == "src/stage.ts").ShouldBeFalse();
    [Fact] void should_explain_why_shared_ui_is_not_published() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-006").ShouldBeTrue();
}
