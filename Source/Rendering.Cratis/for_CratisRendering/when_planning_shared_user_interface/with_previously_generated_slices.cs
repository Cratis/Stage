// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class with_previously_generated_slices : given.a_partitioned_screen_plan
{
    static readonly string[] _aggregates = ["scene.json", "src/bindings.ts", "src/stage-scene.json", "src/stage.ts"];

    void Because()
    {
        _application = AppPlan(_profile);
        _result = CratisRendering.PlanFrom(
            _loaded,
            new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]),
            _options with { UserInterface = new([PlanSelectionEntry.Slice("Projects", "Registration", "ProjectLookup")]) });
    }

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_emit_every_shared_scene_aggregate() => _aggregates.All(path => _result.Artifacts.Any(artifact => artifact.RelativePath == path)).ShouldBeTrue();
    [Fact] void should_match_the_application_aggregates_when_every_slice_is_present() => _aggregates.Select(path => _result.Artifacts.Single(artifact => artifact.RelativePath == path).Sha256).ShouldEqual(_aggregates.Select(path => _application.Artifacts.Single(artifact => artifact.RelativePath == path).Sha256));
    [Fact] void should_still_emit_only_the_selected_slice_business_file() => _result.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/ProjectLookup.cs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_emit_the_application_scaffold() => _result.Artifacts.Any(artifact => artifact.RelativePath == "Program.cs").ShouldBeFalse();
    [Fact] void should_not_explain_an_omission() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-006").ShouldBeFalse();
}
