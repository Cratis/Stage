// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
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
    [Fact] void should_refuse_stage_execution() => _stage.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_translation_refusal() => _stage.Results.Single().Unsupported!.Details.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_refuse_rendering_by_construct() => _rendered.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-024", "STAGE-ESM-032", "STAGE-ESM-033"]);
    [Fact] void should_emit_no_generated_specifications_or_other_artifacts() => _rendered.Artifacts.ShouldBeEmpty();
}
