// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_literal_every_and_join : a_projected_specification
{
    const string Source = """
        policy Access
          require role "Staff"
        module Billing
          feature Invoices
            slice StateChange Registration
              command RegisterInvoice
                invoiceId String identifier
                description String
                produces InvoiceRegistered
                  for invoiceId
                  description = description
              command NameCompany
                company String identifier
                name String
                produces CompanyNamed
                  for company
                  name = name
              event InvoiceRegistered
                description String
              event CompanyNamed
                name String
              specification JoiningAnInvoice
                given actor "Staff"
                {{givens}}
                when command RegisterInvoice
                  invoiceId "invoice-other"
                  description "Other"
                then event InvoiceRegistered for "invoice-other"
                  description "Other"
                then readmodel InvoiceView for "invoice-1"
                  invoiceId "invoice-1"
                  company "company"
                  joined "Joined"
                  label "fixed"
                  score 12.5
            slice StateView Lookup
              readmodel InvoiceView
                invoiceId String
                company String
                joined String?
                label String
                score Decimal?
              query InvoiceById => InvoiceView?
                by invoiceId String
                authorize Access
              projection InvoiceViews => InvoiceView
                no automap
                from InvoiceRegistered
                  invoiceId = $eventSourceId
                  company = description
                join companyDetails on company
                  with CompanyNamed
                    joined = name
                every
                  company = "company"
                  label = "fixed"
                  score = 12.5
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_preserve_the_from_dependency_when_every_overwrites_the_join_key(bool joinedBeforeFrom)
    {
        _plan = PlanFor(joinedBeforeFrom);

        await Run();

        AssertAgreement();
    }

    static SemanticExecutionPlan PlanFor(bool joinedBeforeFrom)
    {
        // The when command creates a different instance, so it cannot repair a missing backfill on invoice-1.
        const string registration = "given event InvoiceRegistered for \"invoice-1\"\n          description \"local\"";
        const string joined = "given event CompanyNamed for \"company\"\n          name \"Joined\"";
        var givens = joinedBeforeFrom ? $"{joined}\n        {registration}" : $"{registration}\n        {joined}";
        return Compile(Source.Replace("{{givens}}", givens, StringComparison.Ordinal));
    }

    static SemanticExecutionPlan Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("every-join"), "every-join", "EveryJoin.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var execution = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        return execution.Plan!;
    }

    void AssertAgreement()
    {
        Assert.True(_reference.Passed, string.Join("; ", _reference.Failures));
        Assert.True(_result.Outcome == SemanticSpecificationOutcome.Passed, string.Join("; ", _result.Failures));
    }
}
