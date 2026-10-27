// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.given;

public class an_application_trigger_reaction : Specification
{
    internal const string Source = """
        trigger BatchRequested
          batchId String
        module Work
          feature Processing
            slice Automation Batches
              event BatchStarted
                batchId String
              reaction ProcessBatch
                when BatchRequested
                  batchId
                  produces BatchStarted
                    for batchId
                    batchId = batchId
        """;

    internal static string InvocationSource => Source.Replace("      reaction ProcessBatch", "      command FinishBatch\n        batchId String identifier\n        produces event BatchFinished\n          for batchId\n          batchId String = batchId\n      reaction ProcessBatch", StringComparison.Ordinal)
        .Replace("          produces BatchStarted\n            for batchId\n            batchId = batchId", "          invokes FinishBatch\n            batchId = batchId", StringComparison.Ordinal);

    private protected ExecutableSemanticModel _model = null!;
    private protected SemanticApplicationContext _context = null!;
    private protected LocatedSemanticSlice _located = null!;
    protected SemanticReaction _reaction = null!;
    protected SemanticReactionTrigger _trigger = null!;
    protected ArtifactRenderDiagnostic[] _diagnostics = [];
    private protected ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(Source);
        SetContext(_model);
        _reaction = _located.Slice.Reactions.Single();
        _trigger = _reaction.Triggers.Single();
    }

    private protected void SetContext(ExecutableSemanticModel model)
    {
        _model = model;
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Reactions", new("Reactions", "Reactions")), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, new("Reactions", "Reactions"));
        _located = _context.SelectedSlices().Single();
    }

    private protected void InspectSource(string source)
    {
        SetContext(for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source));
        _reaction = _located.Slice.Reactions.Single();
        _trigger = _reaction.Triggers.Single();
        Inspect();
    }

    private protected void Plan() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, _context.Request.Scope, new("Reactions", "Reactions"));

    private protected void Inspect()
    {
        var slice = _located.Slice with { Reactions = [_reaction with { Triggers = [_trigger] }] };
        _diagnostics = [.. SemanticVersionFeatureAdmission.Verify(_context, [_located with { Slice = slice }])];
    }
}
