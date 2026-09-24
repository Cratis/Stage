// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Verifies that ambiguous constraint histories and role claims never produce artifacts.
/// </summary>
public class when_rejecting_constraint_and_role_mismatches
{
    [Fact]
    public void should_reject_multiple_constrained_events_in_one_command()
    {
        var source = ConstrainedInvoice().Replace("        produces InvoiceIssued\n          for streamReference\n          description = description",
            "        produces InvoiceIssued\n          for streamReference\n          description = description\n        produces InvoiceIssued\n          for streamReference\n          description = description",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-014" && diagnostic.Message.Contains("multiple claims", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_reject_givens_that_collide_before_the_command()
    {
        var source = ConstrainedInvoice().Replace(
            "      specification IssuingSecondInvoice\n",
            """
                  specification DuplicatedHistory
                    given InvoiceIssued
                      for "invoice-one"
                      description = "First payload"
                    given InvoiceIssued
                      for "invoice-two"
                      description = "First payload"
                    when IssueInvoice
                      description = "Other payload"
                      streamReference = "invoice-three"
                    then InvoiceIssued
                      description = "Other payload"
                  specification IssuingSecondInvoice
            """ + "\n",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("Given events violate", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_reject_role_uri_as_a_policy_claim(bool uppercase)
    {
        var uri = uppercase ? ClaimTypes.Role.ToUpperInvariant() : ClaimTypes.Role;
        var source = when_rendering_portable_authorization.Source.Replace("claim \"department\" matches \"Sales\"", $"claim \"{uri}\" matches \"Sales\"", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Message.Contains("role claim URI", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_reject_role_uri_in_a_caller_fixture()
    {
        var source = when_rendering_portable_authorization.Source.Replace(
            "    slice StateView Lookup",
            IndentTwo("""
                specification RoleClaimFixture
                  given caller
                    authenticated
                    claim "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" = "Staff"
                  when IssueInvoice
                    invoiceId = "invoice-one"
                    description = "North"
                  then denied
                slice StateView Lookup
            """),
            StringComparison.Ordinal).Replace("\n      slice StateView Lookup", "\n    slice StateView Lookup", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("role claim URI", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    static string ConstrainedInvoice() => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
        .Replace(
            "      event InvoiceIssued\n",
            """
                  constraint UniqueDescription
                    unique description on InvoiceIssued
                  event InvoiceIssued
            """ + "\n",
            StringComparison.Ordinal);

    static string IndentTwo(string text) => "  " + text.Replace("\n", "\n  ", StringComparison.Ordinal);
}
