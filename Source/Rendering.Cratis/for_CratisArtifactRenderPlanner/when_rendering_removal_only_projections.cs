// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Renders a projection that only removes instances the read model already holds.
/// </summary>
public class when_rendering_removal_only_projections : Specification
{
    const string Source = """
        module Billing
          feature Invoices
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String
              query InvoiceById => InvoiceView?
                by invoiceId String
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
        """;

    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("removal-only"), "removal-only", "Removal.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Billing", "Billing"));
    }

    [Fact] void should_render_the_projection() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_render_the_removal() => Code().ShouldContain("builder.RemovedWith<");
    [Fact] void should_not_render_a_from_block() => Code().ShouldNotContain("builder.From<");

    string Code() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Billing/Invoices/InvoiceLookup/InvoiceLookup.cs").Bytes.AsSpan());
}
