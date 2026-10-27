// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTriggerArtifactRenderer.given;

public class an_application_trigger_reaction : Specification
{
    protected const string Source = for_SemanticOccurrenceAdmission.given.an_application_trigger_reaction.Source;
    private protected ArtifactRenderPlan _plan = null!;
    private protected ExecutableSemanticModel _model = null!;
    private protected SemanticApplicationContext _context = null!;
    private protected LocatedSemanticSlice _located = null!;
    private protected SemanticReaction _reaction = null!;
    private protected SemanticReactionTrigger _trigger = null!;

    void Establish()
    {
        SetContext(for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(
            for_SemanticOccurrenceAdmission.given.an_application_trigger_reaction.Source));
        _reaction = _located.Slice.Reactions.Single();
        _trigger = _reaction.Triggers.Single();
    }

    private protected void Plan() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Reactions", "Reactions"));

    private protected void SetContext(ExecutableSemanticModel model)
    {
        _model = model;
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Reactions", new("Reactions", "Reactions")), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, new("Reactions", "Reactions"));
        _located = _context.SelectedSlices().Single();
    }
}
