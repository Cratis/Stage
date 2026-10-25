// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class with_only_the_selected_slice : given.a_partitioned_screen_plan
{
    CratisPlanResult _again = null!;

    void Because()
    {
        var selection = new PlanSelection([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]);
        var options = _options with { UserInterface = new([]) };
        _result = CratisRendering.PlanFrom(_loaded, selection, options);
        _again = CratisRendering.PlanFrom(_loaded, selection, options);
    }

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_compose_the_screen_of_the_selected_slice() => Text(Artifact("scene.json")).ShouldContain("StaticOverview");
    [Fact] void should_not_compose_the_screen_of_a_slice_that_is_not_present() => Text(Artifact("scene.json")).ShouldNotContain("LookupOverview");
    [Fact] void should_bind_the_command_of_the_selected_slice() => Text(Artifact("src/bindings.ts")).ShouldContain("RegisterProject");
    [Fact] void should_not_import_the_proxy_of_a_slice_that_is_not_present() => Text(Artifact("src/bindings.ts")).ShouldNotContain("ProjectLookup");
    [Fact] void should_not_route_a_command_of_a_slice_that_is_not_present() => Text(Artifact("src/stage.ts")).ShouldNotContain("ProjectLookup");
    [Fact] void should_render_the_stage_document_from_the_composed_scene() => Text(Artifact("src/stage-scene.json")).ShouldNotContain("LookupOverview");
    [Fact] void should_be_deterministic() => _again.Digest.ShouldEqual(_result.Digest);
    [Fact] void should_not_explain_an_omission() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-006").ShouldBeFalse();

    Contracts.Rendering.PlannedArtifact Artifact(string path) => _result.Artifacts.Single(artifact => artifact.RelativePath == path);
}
