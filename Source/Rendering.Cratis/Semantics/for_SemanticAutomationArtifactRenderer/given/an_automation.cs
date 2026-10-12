// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;

public class an_automation : Specification
{
    internal const string Source = """
        module Work
          feature Processing
            slice StateChange Start
              command StartWork
                id String identifier
                amount Decimal
                produces event Started
                  for id
                  id String = id
                  amount Decimal = amount
            slice Automation FollowUp
              command CompleteWork
                id String identifier
                amount Decimal
                note String optional
                produces event Completed
                  for id
                  amount Decimal = amount
              event Acknowledged
                scheduledAt DateTime
              reaction CompletePositive
                where amount > 0
                when Started
                  invokes CompleteWork
                    id = id
                    amount = amount
              reaction Acknowledge
                when Started
                  produces Acknowledged
                    scheduledAt = $context.occurred
        """;

    private protected ExecutableSemanticModel _model = null!;
    private protected SemanticApplicationContext _context = null!;
    private protected LocatedSemanticSlice _located = null!;
    private protected ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _model = Compile(Source);
        SetContext(_model);
    }

    private protected void SetContext(ExecutableSemanticModel model)
    {
        _model = model;
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Reactions", new("Reactions", "Reactions")), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, new("Reactions", "Reactions"));
        _located = _context.SelectedSlices().Single(slice => slice.Slice.Kind == SemanticSliceKind.Automation);
    }

    internal static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Reactions"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("reactions"), "reactions", "Reactions.play", source);
        var compilation = new SemanticModelCompiler().Compile("Reactions", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        return compilation.Value!.Model;
    }

    private protected void ReplaceAutomation(SemanticSlice slice)
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        SetContext(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(existing => existing.Id == slice.Id ? slice : existing)] }] }]
        }));
    }

    private protected void Plan() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, _context.Request.Scope, new("Reactions", "Reactions"));
}
