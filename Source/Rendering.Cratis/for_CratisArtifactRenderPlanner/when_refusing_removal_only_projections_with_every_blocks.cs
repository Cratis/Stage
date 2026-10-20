// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Keeps refusing a root without a from block when it does more than remove instances.
/// </summary>
public class when_refusing_removal_only_projections_with_every_blocks : Specification
{
    const string Source = """
        module Billing
          feature Invoices
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String
                lastSeen String
              query InvoiceById => InvoiceView?
                by invoiceId String
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
                every
                  lastSeen = $eventSourceId
        """;

    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("removal-every"), "removal-every", "Removal.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Billing", "Billing"));
    }

    [Fact] void should_not_render() => _plan.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_projection_shape() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-017").ShouldBeTrue();
}
