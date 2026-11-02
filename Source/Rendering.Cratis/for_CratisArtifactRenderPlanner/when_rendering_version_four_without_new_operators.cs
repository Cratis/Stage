// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Version four is strictly additive: a source without add, subtract, increment, decrement or clear projections renders exactly as version three.
/// </summary>
public class when_rendering_version_four_without_new_operators
{
    [Fact]
    public void should_render_flat_projections_as_version_three_does()
    {
        var versionThree = when_rendering_model_bound_projections.Code(when_rendering_model_bound_projections.Plan(when_rendering_model_bound_projections.Source, when_rendering_model_bound_running_totals.VersionThree()));
        var versionFour = when_rendering_model_bound_projections.Code(when_rendering_model_bound_projections.Plan(when_rendering_model_bound_projections.Source, true));
        Assert.Equal(versionThree, versionFour);
    }

    [Fact]
    public void should_render_every_artifact_of_a_source_with_fluent_shapes_as_version_three_does()
    {
        const string source = when_rendering_scoped_projections.ScopedSource;
        var versionThree = when_rendering_model_bound_projections.Plan(source, when_rendering_model_bound_running_totals.VersionThree());
        var versionFour = when_rendering_model_bound_projections.Plan(source, true);
        Assert.True(versionThree.Success && versionFour.Success);
        Assert.Equal(Describe(versionThree), Describe(versionFour));
    }

    // The persisted configuration records the version itself, so it is the one artifact that legitimately differs.
    static IEnumerable<(string Path, string Bytes)> Describe(ArtifactRenderPlan plan) =>
        plan.Artifacts.Where(artifact => artifact.RelativePath != ReducerConventionInput.RelativePath)
            .Select(artifact => (artifact.RelativePath, Convert.ToHexString(artifact.Bytes.AsSpan())));
}
#endif
