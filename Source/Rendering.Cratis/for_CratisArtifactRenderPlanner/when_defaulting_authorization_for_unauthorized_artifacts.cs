// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A model with no <c>authorize</c> clause renders <c>[Authorize]</c>, reports the inferred default, and runs its
/// caller-less specifications as the labelled authenticated default caller.
/// </summary>
public class when_defaulting_authorization_for_unauthorized_artifacts : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _command = string.Empty;
    string _query = string.Empty;
    string _specification = string.Empty;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource) + "\n" + """
            slice StateView ListInvoices
              readmodel Invoice
                id String key
                description String
              query AllInvoices => Invoice optional
                by id String
              projection Invoices => Invoice
                from InvoiceIssued key $eventSourceId
                  description = description
        """;
        _plan = invoice_model.Plan(invoice_model.Compile(source));
        _command = Text(artifact => artifact.RelativePath.EndsWith("/Issue.cs", StringComparison.Ordinal));
        _query = Text(artifact => artifact.RelativePath.EndsWith("/ListInvoices.cs", StringComparison.Ordinal));
        _specification = Text(artifact => artifact.RelativePath.EndsWith("/when_issuing_first_invoice.cs", StringComparison.Ordinal));
    }

    [Fact] void should_plan_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_command_requiring_an_authenticated_caller() => _command.ShouldContain("[Authorize]");
    [Fact] void should_not_render_anonymous_access() => _command.ShouldNotContain("AllowAnonymous");
    [Fact] void should_render_the_query_requiring_an_authenticated_caller() => _query.ShouldContain("[Authorize]");
    [Fact] void should_not_render_anonymous_query_access() => _query.ShouldNotContain("AllowAnonymous");
    [Fact] void should_report_exactly_two_inferred_defaults() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-054").ShouldEqual(2);
    [Fact] void should_report_one_inferred_default_for_each_artifact() => _plan.Diagnostics.Where(diagnostic =>
        diagnostic.Code == "STAGE-ESM-054" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information)
        .Select(diagnostic => diagnostic.Artifact).Distinct().Count().ShouldEqual(2);
    [Fact] void should_label_the_default_caller_in_the_specification() => _specification.ShouldContain(Semantics.SemanticDefaultCaller.Comment);
    [Fact] void should_run_the_specification_as_an_authenticated_principal() =>
        _specification.ShouldContain("ClaimsIdentity([], \"Screenplay\")");

    string Text(Func<PlannedArtifact, bool> match) => Encoding.UTF8.GetString(_plan.Artifacts.Single(match).Bytes.AsSpan());
}
