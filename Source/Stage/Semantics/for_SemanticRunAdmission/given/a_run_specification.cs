// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission.given;

/// <summary>
/// A compiled model whose single specification is mutated and then checked for run admission.
/// </summary>
public class a_run_specification : Specification
{
    protected const string Invoicing = """
        module Billing
          feature Payments
            slice StateChange Issue
              command IssueInvoice
                invoiceId String identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  description = description
              event InvoiceIssued
                description String
              specification Issuing
                when IssueInvoice
                  invoiceId = "one"
                  description = "First"
                then InvoiceIssued
                  description = "First"
        """;

    private protected SemanticExecutionPlan _plan = null!;
    private protected SemanticSpecification _specification = null!;
    private protected SemanticUnsupportedCapability? _refusal;

    protected void Prepare(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RunAdmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("RunAdmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        _specification = _plan.Specifications.Values.Single();
    }

    protected void Check() => _refusal = SemanticRunAdmission.Check(_plan, _specification);

    protected string Refusal() => _refusal is null ? "<admitted>" : $"{_refusal.Capability}|{_refusal.Construct}|{_refusal.Details}";
}
