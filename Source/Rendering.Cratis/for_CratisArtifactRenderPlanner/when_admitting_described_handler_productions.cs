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

public class when_admitting_described_handler_productions : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderDiagnostic[] _diagnostics = [];
    SemanticId _commandId;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan());
        _model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Name = "Described" });
        _commandId = _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Commands).Single(command => command.Name == "ApplyLateFee").Id;
    }

    void Because()
    {
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Described", "Described"));
        _diagnostics = [.. plan.Diagnostics.Where(diagnostic => diagnostic.Artifact == _commandId)];
    }

    [Fact] void should_refuse_the_described_handler() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-IMPL-002");
    [Fact] void should_not_refuse_the_handler_owned_production() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-006");
}
