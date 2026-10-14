// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_screen_fragments;

public class with_a_feature_selection : given.a_partitioned_screen_plan
{
    void Because()
    {
        _application = AppPlan(_profile);
        _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Feature("Projects", "Registration"), PlanSelectionEntry.Slice("Projects", "Registration", "ProjectLookup")]), _options);
    }

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_union_both_fragments_without_duplicates() => Fragments(_result.Artifacts).Count().ShouldEqual(2);
    [Fact] void should_match_the_application_fragment_bytes() => Fragments(_result.Artifacts).Select(artifact => artifact.Sha256).ShouldEqual(Fragments(_application.Artifacts).Select(artifact => artifact.Sha256));
    [Fact] void should_keep_the_canonical_application_scene_byte_identical() => _application.Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Sha256.ShouldEqual(AppPlan(_legacy).Artifacts.Single(artifact => artifact.RelativePath == "scene.json").Sha256);
    [Fact] void should_keep_the_enriched_stage_document_byte_identical() => _application.Artifacts.Single(artifact => artifact.RelativePath == "src/stage-scene.json").Sha256.ShouldEqual(AppPlan(_legacy).Artifacts.Single(artifact => artifact.RelativePath == "src/stage-scene.json").Sha256);
    [Fact] void should_plan_identical_inputs_deterministically() => AppPlan(CratisRendering.WithScreenPartitions(_legacy, _compilation)).Digest.ShouldEqual(_application.Digest);
    [Fact] void should_keep_legacy_callers_without_fragments() => Fragments(AppPlan(_legacy).Artifacts).ShouldBeEmpty();
}
