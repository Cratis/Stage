// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_a_model_with_slice_local_plan_issues : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _supported = null!;
    ArtifactRenderPlan _refused = null!;

    void Establish() => _loaded = when_loading_a_model_with_slice_local_plan_issues.Create().Loaded!;

    void Because()
    {
        var slices = _loaded.Model.Application.Modules.Single().Features.Single().Slices;
        _supported = CratisRendering.Plan(_loaded.Model, _loaded.Plan, new(ArtifactRenderScopeKind.Slice, slices.Single(slice => slice.Name == "RegisterProject").Id), new("Projects", "Projects"));
        _refused = CratisRendering.Plan(_loaded.Model, _loaded.Plan, new(ArtifactRenderScopeKind.Slice, slices.Single(slice => slice.Name == "Lookup").Id), new("Projects", "Projects"));
    }

    [Fact] void should_render_the_supported_slice() => Assert.True(_supported.Success, string.Join("; ", _supported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_emit_the_supported_command() => _supported.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("RegisterProject.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_the_query_slice() => _refused.Success.ShouldBeFalse();
    [Fact] void should_identify_the_required_query_refusal() => Assert.Contains(_refused.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-010" && diagnostic.Artifact == _loaded.Plan.Queries.Keys.Single());
}
