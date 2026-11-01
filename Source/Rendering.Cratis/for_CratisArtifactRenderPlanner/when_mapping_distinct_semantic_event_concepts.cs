// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_mapping_distinct_semantic_event_concepts : Specification
{
    [Theory]
    [InlineData(false, "Uuid", "Uuid")]
    [InlineData(true, "Uuid", "Uuid")]
    [InlineData(false, "Int", "Decimal")]
    [InlineData(true, "Int", "Decimal")]
    public void should_convert_accepted_concepts_or_retain_compiler_type_rejections(bool wrapped, string sourcePrimitive, string targetPrimitive)
    {
        var occurrenceMapping = wrapped ? "          occurredAt = $context.occurred\n" : string.Empty;
        var occurrenceProperty = wrapped ? "        occurredAt DateTime\n" : string.Empty;
        var source = $$"""
            concept OrderId : {{sourcePrimitive}}
            concept MovementId : {{targetPrimitive}}
            module Inventory
              feature Stock
                slice StateChange Move
                  command MoveStock
                    id String identifier
                    orderId OrderId
                    produces StockMoved
                      for id
                      movementId = orderId
            {{occurrenceMapping}}      event StockMoved
                    movementId MovementId
            {{occurrenceProperty}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvoiceModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoices"), "invoices", "Invoices.play", source);
        var compilation = new SemanticModelCompiler().Compile("InvoiceModel", SemanticDocumentSet.Create([document], catalog));
        if (!string.Equals(sourcePrimitive, targetPrimitive, StringComparison.Ordinal))
        {
            Assert.False(compilation.Success);
            Assert.Contains(compilation.Diagnostics, diagnostic => string.Equals(diagnostic.Code, "PLAY0273", StringComparison.Ordinal));
            return;
        }

        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var plan = invoice_model.Plan(compilation.Value!.Model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var artifact = plan.Artifacts.Single(candidate => string.Equals(candidate.RelativePath, "Inventory/Stock/Move/Move.cs", StringComparison.Ordinal));
        var content = Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
        Assert.Contains("new MovementId(OrderId.Value)", content, StringComparison.Ordinal);
        if (wrapped)
        {
            Assert.Contains("EventForEventSourceId", content, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("public StockMoved Handle()", content, StringComparison.Ordinal);
        }
    }
}
