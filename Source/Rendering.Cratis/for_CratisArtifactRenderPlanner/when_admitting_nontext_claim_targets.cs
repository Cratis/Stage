// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_nontext_claim_targets : Specification
{
    internal const string Source = """
        concept InvoiceId : Decimal
        policy Owns
          require claim "owner" matches subject or role "Staff"
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                authorize Owns
                invoiceId InvoiceId identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              event InvoiceIssued
                invoiceId InvoiceId
                description String
        """;

    internal const string QuerySource = Source + "\n" + """
            slice StateView Lookup
              readmodel InvoiceSummary
                invoiceId InvoiceId
                description String
              query InvoiceById => InvoiceSummary?
                by invoiceId InvoiceId
                authorize Owns
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceIssued key invoiceId
                  invoiceId = invoiceId
                  description = description
        """;

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_render_nontext_command_and_query_subject_claim_terms_as_false(string primitive)
    {
        var source = QuerySource.Replace("concept InvoiceId : Decimal", $"concept InvoiceId : {primitive}", StringComparison.Ordinal);
        var policy = RenderedPolicy(source);
        policy.Split('\n').Count(line => line.Contains("FromResult((false || context.Principal.IsInRole(\"Staff\")))", StringComparison.Ordinal)).ShouldEqual(2);
        policy.Contains("PolicyValues.Match(", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Decimal", false)]
    [InlineData("Int", false)]
    [InlineData("Bool", false)]
    [InlineData("Decimal", true)]
    [InlineData("Int", true)]
    [InlineData("Bool", true)]
    public void should_render_nontext_artifact_claim_terms_as_false(string primitive, bool concept)
    {
        var source = Source.Replace("concept InvoiceId : Decimal", "concept InvoiceId : String", StringComparison.Ordinal)
            .Replace("matches subject", "matches value", StringComparison.Ordinal)
            .Replace("description String\n        produces", $"description String\n        value {(concept ? "Scalar" : primitive)}\n        produces", StringComparison.Ordinal);
        if (concept) source = $"concept Scalar : {primitive}\n" + source;
        var policy = RenderedPolicy(source);
        policy.Contains("FromResult((false || context.Principal.IsInRole(\"Staff\")))", StringComparison.Ordinal).ShouldBeTrue();
        policy.Contains("PolicyValues.Match(", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_render_nested_nontext_concept_claim_terms_as_false(string primitive)
    {
        var source = $"concept Scalar : {primitive}\ntype Details\n  value Scalar\n" + Source
            .Replace("concept InvoiceId : Decimal", "concept InvoiceId : String", StringComparison.Ordinal)
            .Replace("matches subject", "matches details.value", StringComparison.Ordinal)
            .Replace("description String\n        produces", "description String\n        details Details\n        produces", StringComparison.Ordinal);
        var policy = RenderedPolicy(source);
        policy.Contains("FromResult((false || context.Principal.IsInRole(\"Staff\")))", StringComparison.Ordinal).ShouldBeTrue();
        policy.Contains("PolicyValues.Match(", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_admit_a_claim_against_a_nontext_subject(string primitive) =>
        invoice_model.Plan(invoice_model.Compile(Source.Replace("concept InvoiceId : Decimal", $"concept InvoiceId : {primitive}", StringComparison.Ordinal))).Success.ShouldBeTrue();

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_admit_a_claim_against_a_nontext_property(string primitive)
    {
        var source = when_rendering_portable_authorization.Source.Replace("description String", $"description {primitive}", StringComparison.Ordinal);
        invoice_model.Plan(invoice_model.Compile(source)).Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Date")]
    [InlineData("DateTime")]
    public void should_admit_text_backed_dates_with_their_canonical_text(string primitive)
    {
        var source = when_rendering_portable_authorization.Source.Replace("description String", $"description {primitive}", StringComparison.Ordinal);
        invoice_model.Plan(invoice_model.Compile(source)).Success.ShouldBeTrue();
    }

    static string RenderedPolicy(string source)
    {
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        plan.Success.ShouldBeTrue();
        return string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/StagePolicy_", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }
}
