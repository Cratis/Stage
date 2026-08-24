// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_semantic_scopes : a_register_project_render_request
{
    ArtifactRenderPlan _application = null!;
    ArtifactRenderPlan _modulePlan = null!;
    ArtifactRenderPlan _featurePlan = null!;
    ArtifactRenderPlan _slicePlan = null!;

    void Because()
    {
        _application = _planner.Plan(_request);
        _modulePlan = _planner.Plan(_request with { Scope = new(ArtifactRenderScopeKind.Module, _module.Id) });
        _featurePlan = _planner.Plan(_request with { Scope = new(ArtifactRenderScopeKind.Feature, _feature.Id) });
        _slicePlan = _planner.Plan(_request with { Scope = new(ArtifactRenderScopeKind.Slice, _registerProject.Id) });
    }

    [Fact] void should_plan_every_scope_successfully() => new[] { _application, _modulePlan, _featurePlan, _slicePlan }.All(_ => _.Success).ShouldBeTrue();
    [Fact] void should_include_both_slices_for_the_module() => SlicePaths(_modulePlan).Count.ShouldEqual(2);
    [Fact] void should_include_both_slices_for_the_feature() => SlicePaths(_featurePlan).Count.ShouldEqual(2);
    [Fact] void should_include_only_the_selected_slice_for_the_slice_scope() => SlicePaths(_slicePlan).ShouldContainOnly(["Projects/Registration/RegisterProject/RegisterProject.cs"]);
    [Fact] void should_use_the_same_slice_path_in_every_containing_scope() => Plans().All(_ => _.Artifacts.Any(artifact => artifact.RelativePath == RegisterProjectPath)).ShouldBeTrue();
    [Fact] void should_use_the_same_slice_bytes_in_every_containing_scope() => Plans().Select(PlanBytes).Skip(1).All(bytes => bytes.SequenceEqual(PlanBytes(_application))).ShouldBeTrue();
    [Fact] void should_only_include_scaffolding_for_the_application_scope() => new[] { _modulePlan, _featurePlan, _slicePlan }.All(_ => _.Artifacts.All(artifact => artifact.RelativePath != "Projects.csproj")).ShouldBeTrue();

    const string RegisterProjectPath = "Projects/Registration/RegisterProject/RegisterProject.cs";

    IReadOnlyList<ArtifactRenderPlan> Plans() => [_application, _modulePlan, _featurePlan, _slicePlan];
    IReadOnlyList<string> SlicePaths(ArtifactRenderPlan plan) => [.. plan.Artifacts.Select(_ => _.RelativePath).Where(_ => _.EndsWith("/RegisterProject.cs", StringComparison.Ordinal) || _.EndsWith("/ProjectLookup.cs", StringComparison.Ordinal))];
    System.Collections.Immutable.ImmutableArray<byte> PlanBytes(ArtifactRenderPlan plan) => plan.Artifacts.Single(_ => _.RelativePath == RegisterProjectPath).Bytes;
}
