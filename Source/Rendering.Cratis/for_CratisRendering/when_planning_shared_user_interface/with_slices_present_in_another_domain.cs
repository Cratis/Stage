// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

/// <summary>
/// An application generated one domain at a time: this plan generates under Sales while the lookup was generated
/// under Support. Planning the other domain against the same state yields the same shared UI.
/// </summary>
public class with_slices_present_in_another_domain : given.a_partitioned_screen_plan
{
    static readonly string[] _aggregates = ["scene.json", "src/bindings.ts", "src/stage-scene.json", "src/stage.ts"];

    CratisPlanResult _other = null!;

    void Because()
    {
        _result = CratisRendering.PlanFrom(
            _loaded,
            new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]),
            _options with
            {
                Domain = "Sales",
                UserInterface = new([]),
                UserInterfaceDomains = [new("Support", new([PlanSelectionEntry.Slice("Projects", "Registration", "ProjectLookup")]))]
            });
        _other = CratisRendering.PlanFrom(
            _loaded,
            new([PlanSelectionEntry.Slice("Projects", "Registration", "ProjectLookup")]),
            _options with
            {
                Domain = "Support",
                UserInterface = new([]),
                UserInterfaceDomains = [new("Sales", new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]))]
            });
    }

    [Fact] void should_admit_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_admit_the_plan_of_the_other_domain() => _other.Success.ShouldBeTrue();
    [Fact] void should_import_the_command_from_its_own_domain() => Text(Artifact(_result, "src/bindings.ts")).ShouldContain("'../Sales/Projects/Registration/RegisterProject'");
    [Fact] void should_import_the_query_from_the_domain_it_was_generated_under() => Text(Artifact(_result, "src/bindings.ts")).ShouldContain("'../Support/Projects/Registration/ProjectLookup'");
    [Fact] void should_not_import_the_query_from_its_own_domain() => Text(Artifact(_result, "src/bindings.ts")).ShouldNotContain("'../Sales/Projects/Registration/ProjectLookup'");
    [Fact] void should_compose_the_screen_of_the_other_domain() => Text(Artifact(_result, "scene.json")).ShouldContain("LookupOverview");
    [Fact] void should_still_emit_only_the_selected_slice_business_file() => _result.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/ProjectLookup.cs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_emit_the_same_shared_user_interface_whichever_domain_is_generated() => _aggregates.Select(path => Artifact(_result, path).Sha256).ShouldEqual(_aggregates.Select(path => Artifact(_other, path).Sha256));

    static PlannedArtifact Artifact(CratisPlanResult result, string path) => result.Artifacts.Single(artifact => artifact.RelativePath == path);
}
