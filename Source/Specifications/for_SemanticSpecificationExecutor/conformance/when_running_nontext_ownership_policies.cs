// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;

public class when_running_nontext_ownership_policies : Specification
{
    readonly List<string> _differences = [];
    int _executed;

    protected async Task Because()
    {
        foreach (var (primitive, value, claim) in new[] { ("Decimal", "123.45", "123.45"), ("Int", "123", "123"), ("Bool", "true", "true") })
        {
            var source = $$"""
                concept InvoiceId : {{primitive}}
                policy Owns
                  require (claim "owner" matches subject or claim "owner" matches invoiceId) or role "Staff"
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
                      specification DenyingTheTextClaim
                        given caller
                          authenticated
                          claim "owner" = "{{claim}}"
                        when IssueInvoice
                          invoiceId = {{value}}
                          description = "North"
                        then denied
                      specification AllowingTheAlternativeRole
                        given caller
                          authenticated
                          role "Staff"
                          claim "owner" = "{{claim}}"
                        when IssueInvoice
                          invoiceId = {{value}}
                          description = "North"
                        then InvoiceIssued
                          invoiceId = {{value}}
                          description = "North"
                """;
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("nontext-claims"), "nontext-claims", "NontextClaims.play", source);
            var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
            compilation.Success.ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
            foreach (var specification in plan.Specifications.Values)
            {
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
                _executed++;
                if (!reference.Passed || result.Outcome != SemanticSpecificationOutcome.Passed || result.ExecutionKind != reference.Execution.Kind.ToString())
                {
                    _differences.Add($"{primitive}/{specification.Name}: reference {reference.Execution.Kind}/{reference.Passed} {string.Join(';', reference.Failures)}; Stage {result.Outcome}/{result.ExecutionKind} {string.Join(';', result.Failures)} {result.Unsupported?.Details}");
                }
            }
        }
    }

    [Fact]
    public void should_execute_all_scalar_denial_and_role_alternative_vectors() => _executed.ShouldEqual(6);
    [Fact]
    public void should_match_the_reference_for_numeric_and_boolean_subjects_and_properties() => Assert.True(_differences.Count == 0, string.Join(Environment.NewLine, _differences));
}
