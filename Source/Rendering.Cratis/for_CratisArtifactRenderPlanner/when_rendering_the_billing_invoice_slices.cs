// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>Proves that the Billing IssueInvoice and CancelInvoice slices render, with their projection and generated specifications.</summary>
public class when_rendering_the_billing_invoice_slices : Specification
{
    CratisPlanResult _result = null!;

    async Task Because()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".git")) && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        _result = await CratisRendering.PlanFrom(
            new PlaySources(Path.Combine(directory.FullName, "Samples", "Billing"), []),
            new PlanSelection([
                PlanSelectionEntry.Slice("Invoicing", "Invoices", "IssueInvoice"),
                PlanSelectionEntry.Slice("Invoicing", "Invoices", "CancelInvoice")]),
            new CratisPlanOptions("Billing", "Billing", "Billing"));
    }

    [Fact]
    void should_have_no_error_diagnostics() =>
        Assert.True(
            _result.Diagnostics.All(_ => _.Severity != ArtifactRenderDiagnosticSeverity.Error),
            string.Join(Environment.NewLine, _result.Diagnostics.Where(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(_ => $"{_.Code}: {_.Message}")));

    [Fact]
    void should_render_the_invoice_state_projection() =>
        Assert.Contains(_result.Artifacts, _ => _.RelativePath.EndsWith("InvoiceStateProjection.cs", StringComparison.Ordinal));

    [Fact]
    void should_generate_specifications_for_issue_invoice() =>
        Assert.Contains(_result.Artifacts, _ => _.RelativePath.Contains("IssueInvoice", StringComparison.Ordinal) && _.RelativePath.Contains("Spec", StringComparison.OrdinalIgnoreCase));

    [Fact]
    void should_generate_specifications_for_cancel_invoice() =>
        Assert.Contains(_result.Artifacts, _ => _.RelativePath.Contains("CancelInvoice", StringComparison.Ordinal) && _.RelativePath.Contains("Spec", StringComparison.OrdinalIgnoreCase));

    [Fact]
    void should_compile_the_rendered_sources_without_errors()
    {
        var files = _result.Artifacts
            .Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
            .Select(_ => new RenderedFile(_.RelativePath, System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())));
        var errors = RenderedOutput.CreateCompilation(files).GetDiagnostics().Where(_ => _.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Take(20)));
    }
}
#endif
