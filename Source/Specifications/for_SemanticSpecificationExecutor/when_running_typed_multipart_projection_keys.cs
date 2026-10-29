// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_typed_multipart_projection_keys : Specification
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("TypedMultipart"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("TypedMultipart", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
    }

    [Fact] void should_run_each_specification() => _report.Results.Count.ShouldEqual(2);
    [Fact] void should_preserve_uuid_and_text_concept_parts() => Assert.All(_report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, string.Join(';', result.Failures) + result.Unsupported?.Details));
    [Fact] void should_trace_both_distinct_uuid_keys() => _report.Results.Single(result => result.Name == "Found").Trace!.ReadModels.Count.ShouldEqual(2);

    const string Source = """
        concept InvoiceId : Uuid
        concept Tenant : String
        module Billing
          feature Payments
            slice StateView Status
              event Paid
                tenant Tenant
                invoice InvoiceId
                amount Int
              event Cancelled
                tenant Tenant
                invoice InvoiceId
              readmodel Payment
                invoice InvoiceId key
                tenant Tenant key
                amount Int
              projection Payments => Payment
                from Paid
                  key
                    tenant = tenant
                    invoice = invoice
                  amount = amount
                remove with Cancelled
                  key
                    invoice = invoice
                    tenant = tenant
              query ByKey => Payment optional
                by
                  tenant Tenant
                  invoice InvoiceId
              specification Found
                given Paid
                  tenant = "north"
                  invoice = "00000000-0000-0000-0000-000000000001"
                  amount = 1
                given Paid
                  tenant = "north"
                  invoice = "00000000-0000-0000-0000-000000000002"
                  amount = 2
                then query ByKey
                  arguments
                    invoice = "00000000-0000-0000-0000-000000000002"
                    tenant = "north"
                  result
                    invoice = "00000000-0000-0000-0000-000000000002"
                    tenant = "north"
                    amount = 2
              specification Removed
                given Paid
                  tenant = "north"
                  invoice = "00000000-0000-0000-0000-000000000001"
                  amount = 1
                given Paid
                  tenant = "north"
                  invoice = "00000000-0000-0000-0000-000000000002"
                  amount = 2
                when append Cancelled
                  tenant = "north"
                  invoice = "00000000-0000-0000-0000-000000000001"
                then no readmodel Payment for {"tenant":"north","invoice":"00000000-0000-0000-0000-000000000001"}
                then readmodel Payment
                  invoice = "00000000-0000-0000-0000-000000000002"
                  tenant = "north"
                  amount = 2
        """;
}
