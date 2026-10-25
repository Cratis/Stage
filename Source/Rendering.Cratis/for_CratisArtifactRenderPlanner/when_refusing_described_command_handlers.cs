// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_described_command_handlers : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan());
    void Because() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Described", "Described"));

    [Fact] void should_read_the_v23_vector() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact] void should_admit_the_version() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_refuse_the_handler_with_its_identity() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-IMPL-002" && diagnostic.Message.Contains("ApplyLateFee", StringComparison.Ordinal) && diagnostic.Message.Contains(_model.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.Commands).Single(command => command.Handler is not null).Handler!.RequirementId, StringComparison.Ordinal));
    [Fact] void should_omit_the_described_handler_and_compile_retained_artifacts() => given.rendering_refusals.Rejected(_plan, "STAGE-IMPL-002");
    [Fact]
    void should_not_enable_stub_rendering_yet()
    {
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Described", "Described") { ImplementationGaps = ImplementationGapMode.Stub });
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-IMPL-002");
        given.rendering_refusals.Rejected(plan, "STAGE-IMPL-002");
    }
}
