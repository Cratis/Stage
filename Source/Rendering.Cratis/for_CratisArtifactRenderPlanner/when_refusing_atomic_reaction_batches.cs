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

public class when_refusing_atomic_reaction_batches : Specification
{
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        var described = SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan());
        _model = ExecutableSemanticModel.Create(LanguageVersion.V23, SemanticVersion.V23, described.Application with { ReactionProductionMode = SemanticReactionProductionMode.Atomic });
    }

    [Theory]
    [InlineData(ArtifactRenderScopeKind.Application)]
    [InlineData(ArtifactRenderScopeKind.Slice)]
    void should_refuse_the_production_boundary_without_emitting_artifacts(ArtifactRenderScopeKind kind)
    {
        var id = kind == ArtifactRenderScopeKind.Application ? _model.Application.Id : _model.Application.Modules[0].Features[0].Slices[0].Id;
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(kind, id), new("Atomic", "Atomic"));
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-043" && diagnostic.Artifact == _model.Application.Id && diagnostic.Message.Contains("atomic", StringComparison.Ordinal));
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
        plan.Artifacts.ShouldBeEmpty();
    }
}
