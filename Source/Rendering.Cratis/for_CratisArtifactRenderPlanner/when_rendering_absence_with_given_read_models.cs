// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_absence_with_given_read_models : Specification
{
    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    internal static string Source => when_rendering_read_model_absence.Source
        .Replace("given InvoiceIssued\n          for \"first\"", "given readmodel InvoiceView", StringComparison.Ordinal)
        .Replace("given InvoiceIssued\n          for \"second\"", "given readmodel InvoiceView", StringComparison.Ordinal)
        .Replace("specification NoInvoiceYet\n", "specification NoInvoiceYet\n        given readmodel InvoiceView\n          invoiceId = \"second\"\n          description = \"Second\"\n        then readmodel InvoiceView\n          invoiceId = \"second\"\n          description = \"Second\"\n", StringComparison.Ordinal);

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(Source));
        _files = when_rendering_read_model_absence.Files(_plan);
    }

    [Fact] void should_admit_seeded_absence() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_seed_the_scenario() => _files.Any(file => file.Content.Contains(".ReadModel(new ", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_without_errors() => string.Join(Environment.NewLine, RenderedOutput.Errors(_files)).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, RenderedOutput.Warnings(_files)).ShouldEqual(string.Empty);
    [Fact] async Task should_preserve_the_other_seed_and_remove_the_named_seed() => await when_rendering_read_model_absence.RunProjected(when_rendering_read_model_absence.LoadWithArtifacts(_files));
    [Fact] async Task should_fail_absence_for_a_surviving_seed()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Source.Replace("InvoiceView for \"third\"", "InvoiceView for \"second\"", StringComparison.Ordinal)
            .Replace("        then readmodel InvoiceView\n          invoiceId = \"second\"\n          description = \"Second\"\n", string.Empty, StringComparison.Ordinal)));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = when_rendering_read_model_absence.Files(plan);
        string.Join(Environment.NewLine, RenderedOutput.Errors(files)).ShouldEqual(string.Empty);
        var assembly = when_rendering_read_model_absence.LoadWithArtifacts(files);
        var failure = await Catch.Exception(() => when_rendering_read_model_absence.RunProjected(assembly));
        Assert.IsType<global::Xunit.Sdk.XunitException>(Assert.IsType<TargetInvocationException>(failure).InnerException, exactMatch: false);
    }

    [Fact] void should_refuse_partial_seeds()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvoiceModel"));
        var source = Source.Replace("          description = \"Second\"\n", string.Empty, StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoices"), "invoices", "Invoices.play", source);
        var compilation = new SemanticModelCompiler().Compile("InvoiceModel", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeFalse();
        Assert.Contains(compilation.Diagnostics, diagnostic => diagnostic.Code == "PLAY0524");
    }
}
#endif
