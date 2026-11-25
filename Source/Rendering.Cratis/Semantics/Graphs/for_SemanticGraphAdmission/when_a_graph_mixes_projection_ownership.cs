// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphAdmission;

public class when_a_graph_mixes_projection_ownership : Specification
{
    ArtifactRenderPlan _mixed = null!;
    ArtifactRenderPlan _legacy = null!;
    ArtifactRenderPlan _shapeOnly = null!;

    void Because()
    {
        var projections = an_organization_graph_model.Request(graph_projection_sources.Split).Model.Application.GraphProjections;
        _mixed = Plan([.. projections.Select(projection => projection.Name == "MembershipIndex" ? projection with { DeclaringSlice = null } : projection)]);
        _legacy = Plan([.. projections.Select(projection => projection with { DeclaringSlice = null })], legacy: true);
        var shapeOnly = projections[1] with
        {
            DeclaringSlice = null,
            Edges = [.. projections[1].Edges.Select(element => element with { Handlers = [] })]
        };
        _shapeOnly = Plan([projections[0], shapeOnly]);
    }

    static ArtifactRenderPlan Plan(ImmutableArray<SemanticGraphProjection> projections, bool legacy = false)
    {
        var request = an_organization_graph_model.Request(graph_projection_sources.Split);
        var model = ExecutableSemanticModel.Create(legacy ? LanguageVersion.V38 : request.Model.LanguageVersion, legacy ? SemanticVersion.V38 : request.Model.SemanticVersion, request.Model.Application with { GraphProjections = projections });
        return new CratisArtifactRenderPlanner().Plan(request with { Model = model, ExecutionPlan = SemanticExecutionPlan.Compile(model).Plan });
    }

    [Fact] void should_refuse_the_mixed_graph_before_rendering() => _mixed.Success.ShouldBeFalse();
    [Fact] void should_explain_the_inconsistent_ownership() => _mixed.Diagnostics.Any(diagnostic => diagnostic.Code == SemanticGraphAdmission.Unsupported && diagnostic.Message.Contains("Graph 'Organization' mixes projections", StringComparison.Ordinal) && diagnostic.Message.Contains("ownerless handler-bearing projections", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_emit_incomplete_projection_metadata() => _mixed.Artifacts.ShouldBeEmpty();
    [Fact] void should_keep_fully_ownerless_legacy_graphs_renderable() => Assert.True(_legacy.Success, string.Join("; ", _legacy.Diagnostics));
    [Fact] void should_not_require_an_owner_for_a_shape_only_projection() => Assert.True(_shapeOnly.Success, string.Join("; ", _shapeOnly.Diagnostics));
}
#endif
