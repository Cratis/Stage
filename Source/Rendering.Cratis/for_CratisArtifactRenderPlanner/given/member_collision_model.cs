// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class member_collision_model
{
    internal static ArtifactRenderPlan Plan(string commandName, string queryName)
    {
        var source = $$"""
            module Payments
              feature Recording
                slice StateChange RecordPayment
                  command {{commandName}}
                    id String identifier
                    text String
                    validate
                      text not empty
                    produces Recorded
                      for id
                      text = text
                  event Recorded
                    text String
                  specification Recording
                    when {{commandName}}
                      id = "payment-1"
                      text = "paid"
                    then Recorded
                      text = "paid"
                slice StateView Lookup
                  readmodel PaymentTotals
                    id String key
                    total String
                  query {{queryName}} => PaymentTotals?
                    by id String
                  projection Build => PaymentTotals
                    from Recorded
                      id = $eventSourceId
                      total = text
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Payments"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("collision"), "collision", "Collision.play", source);
        var compilation = new SemanticModelCompiler().Compile("Payments", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Payments", "Payments"));
    }

    internal static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
