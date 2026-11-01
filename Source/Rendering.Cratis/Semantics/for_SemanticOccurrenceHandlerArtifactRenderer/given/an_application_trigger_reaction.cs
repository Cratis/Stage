// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceHandlerArtifactRenderer.given;

public class an_application_trigger_reaction : Specification
{
    private protected ArtifactRenderPlan _plan = null!;
    private protected SemanticReaction _reaction = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        _model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(
            for_SemanticOccurrenceAdmission.given.an_application_trigger_reaction.Source);
        _reaction = _model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
    }

    private protected void Plan() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Reactions", "Reactions"));
}
