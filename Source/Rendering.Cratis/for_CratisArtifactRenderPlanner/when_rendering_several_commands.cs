// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_several_commands : Specification
{
    ArtifactRenderPlan _plan = null!;
    string Slice => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Billing/Invoicing/Issue/Issue.cs").Bytes.AsSpan());

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(Source));

    [Fact] void should_render_the_shared_slice() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_render_both_commands() => Slice.ShouldContain("record ReviseInvoice(");
    [Fact] void should_render_the_first_validator() => Slice.ShouldContain("class IssueInvoiceValidator");
    [Fact] void should_render_the_second_validator() => Slice.ShouldContain("class ReviseInvoiceValidator");
    [Fact] void should_render_the_first_response() => Slice.ShouldContain("record IssueInvoiceResponse(");
    [Fact] void should_render_the_second_response() => Slice.ShouldContain("record ReviseInvoiceResponse(");

    internal static string Source => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
        .Replace("      event InvoiceIssued", "      command ReviseInvoice\n        description String\n        streamReference String identifier\n        produces InvoiceIssued\n          for streamReference\n          description = description\n        returns\n          label = description\n      event InvoiceIssued", StringComparison.Ordinal)
        .Replace("        produces InvoiceIssued", "        validate\n          description not empty\n        produces InvoiceIssued", StringComparison.Ordinal)
        .Replace("      command ReviseInvoice", "        returns\n          label = description\n      command ReviseInvoice", StringComparison.Ordinal);
}
