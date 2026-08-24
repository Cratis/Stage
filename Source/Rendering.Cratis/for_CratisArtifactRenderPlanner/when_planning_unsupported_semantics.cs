// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_unsupported_semantics : a_register_project_render_request
{
    const string Source =
        """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                produces ProjectRegistered
                  projectId = projectId
              event ProjectRegistered
                projectId ProjectId
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
        """;

    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("allocated-destination"),
            "allocated-destination",
            "AllocatedDestination.play",
            Source);
        var compilation = new SemanticModelCompiler().Compile(
            "Projects",
            SemanticDocumentSet.Create([document], catalog));
        var model = compilation.Value!.Model;
        var executionPlan = SemanticExecutionPlan.Compile(model).Plan!;
        var request = new ArtifactRenderRequest(
            model,
            executionPlan,
            _request.Profile,
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _plan = _planner.Plan(request);
    }

    [Fact] void should_not_be_publishable() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unsupported_destination() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-006");
    [Fact] void should_report_the_unsupported_affected_instance() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-009");
    [Fact] void should_not_emit_a_semantic_stub() => _plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_not_emit_a_todo() => _plan.Artifacts.All(_ => !Text(_).Contains("TODO", StringComparison.Ordinal)).ShouldBeTrue();
}
