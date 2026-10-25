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
    SemanticId _refusedSlice;
    SemanticId _refusedEvent;

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
        var refused = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        _refusedSlice = refused.Id;
        _refusedEvent = refused.Events.Single().Id;
        var executionPlan = SemanticExecutionPlan.Compile(model).Plan!;
        var request = new ArtifactRenderRequest(
            model,
            executionPlan,
            _request.Profile,
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _plan = _planner.Plan(request);
    }

    [Fact] void should_not_be_publishable() => Assert.Equal(ArtifactRenderOutcome.Partial, _plan.Outcome);
    [Fact] void should_omit_the_destination_slice() => rendering_refusals.Omitted(_plan, _refusedSlice);
    [Fact] void should_omit_the_event_in_the_destination_slice() => rendering_refusals.Omitted(_plan, _refusedEvent);
    [Fact] void should_report_the_unsupported_destination() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-006");

    // Payload-keyed projection syntax is supported; its event belongs to the refused destination slice.
    [Fact] void should_not_refuse_the_payload_keyed_projection() => _plan.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-009");
    [Fact] void should_omit_the_unsupported_destination_and_compile_the_retained_artifacts() => rendering_refusals.Rejected(_plan, "STAGE-ESM-006");
}
