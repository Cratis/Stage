// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_refusing_public_events : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRun _reference = null!;
    SemanticSpecificationRunReport _stage = null!;
    ArtifactRenderPlan _rendered = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan())).Plan!;

    async Task Because()
    {
        _reference = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Keys.Single());
        _stage = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());
        _rendered = CratisRendering.Plan(_plan.Model, _plan, new(ArtifactRenderScopeKind.Application, _plan.Model.Application.Id), new("Contracts", "Contracts"));
    }

    [Fact] void should_pass_the_frozen_reference_specification() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_keep_the_frozen_reference_outcome() => _reference.Execution.Kind.ShouldEqual(PublicEventsCorpus.V9.SpecificationExpectations.Single().Outcome);
    [Fact] void should_execute_stage_translation() => _stage.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference_execution_kind() => _stage.Results.Single().ExecutionKind.ShouldEqual(_reference.Execution.Kind.ToString());
    [Fact] void should_render_the_complete_public_event_translation() => _rendered.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_emit_no_renderer_refusals() => _rendered.Refusals.ShouldBeEmpty();
    [Fact] void should_render_the_public_event_projection_specification() => _rendered.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.Sources.Contains(_plan.Specifications.Keys.Single()));

    [Fact]
    void should_emit_the_inbox_and_outbox_reactors()
    {
        _rendered.Success.ShouldBeTrue();
        _rendered.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("ShipmentTracking.cs", StringComparison.Ordinal));
        _rendered.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("OrderShippedPublisher.cs", StringComparison.Ordinal));
    }

    [Fact]
    void should_keep_capture_overlays_across_public_source_events_refused()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Contracts", "Contracts"));
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
        rendered.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Message.Contains("record overlay across source events", StringComparison.Ordinal));
    }
}
