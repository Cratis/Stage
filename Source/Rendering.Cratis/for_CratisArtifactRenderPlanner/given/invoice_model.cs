// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class invoice_model
{
    public const string TextSource = "\"invoice-one\"";
    public const string OtherTextSource = "\"invoice-two\"";
    public const string UuidSource = "\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"";
    public const string OtherUuidSource = "\"4fa85f64-5717-4562-b3fc-2c963f66afa7\"";

    public static string Source(string type, string first, string second) => $$"""
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                description String
                streamReference {{type}} identifier
                produces InvoiceIssued
                  for streamReference
                  description = description
              event InvoiceIssued
                description String
              specification IssuingFirstInvoice
                when IssueInvoice
                  description = "First payload"
                  streamReference = {{first}}
                then InvoiceIssued
                  description = "First payload"
              specification IssuingSecondInvoice
                when IssueInvoice
                  description = "Second payload"
                  streamReference = {{second}}
                then InvoiceIssued
                  description = "Second payload"
        """;

    public static string WithCompetingIdentity(bool conceptDestination = false)
    {
        var declarations = "concept OtherId : Uuid\n" + (conceptDestination ? "concept InvoiceStream : Uuid\n" : string.Empty);
        var source = conceptDestination
            ? Source("InvoiceStream", UuidSource, OtherUuidSource)
            : Source("String", TextSource, OtherTextSource);
        const string otherIdentity = """
            slice StateChange RegisterOther
              command RegisterOther
                otherId OtherId identifier
                description String
                produces OtherRegistered
                  for otherId
                  description = description
              event OtherRegistered
                description String
        """;
        return declarations + source
            .Replace("command IssueInvoice\n", "command IssueInvoice\n        otherId OtherId\n", StringComparison.Ordinal)
            .Replace("when IssueInvoice\n", "when IssueInvoice\n          otherId = \"5fa85f64-5717-4562-b3fc-2c963f66afa8\"\n", StringComparison.Ordinal) + "\n" + otherIdentity;
    }

    public static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvoiceModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoices"), "invoices", "Invoices.play", source);
        var compilation = new SemanticModelCompiler().Compile("InvoiceModel", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new InvoiceModelCompilationFailed(string.Join(Environment.NewLine, compilation.Diagnostics.Select(_ => $"{_.Code} {_.Severity} {_.Location}: {_.Message}")));
        }

        compilation.Success.ShouldBeTrue();
        return compilation.Value!.Model;
    }

    public static ArtifactRenderPlan Plan(ExecutableSemanticModel model, ArtifactRenderScope? scope = null) =>
        CratisRendering.Plan(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            scope ?? new(ArtifactRenderScopeKind.Application, model.Application.Id),
            new("InvoiceApp", "Invoices"));

    sealed class InvoiceModelCompilationFailed(string diagnostics) : Exception(diagnostics);
}
