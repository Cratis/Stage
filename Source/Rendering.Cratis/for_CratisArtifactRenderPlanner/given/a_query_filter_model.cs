// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_query_filter_model
{
    public const string Source = """
        concept InvoiceId : String
        concept Category : String
          validate
            min 2
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                invoiceId InvoiceId identifier
                status String
                category Category
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  status = status
                  category = category
              event InvoiceIssued
                invoiceId InvoiceId
                status String
                category Category
            slice StateView Lookup
              readmodel InvoiceSummary
                invoiceId InvoiceId
                status String
                category Category
              query AllInvoices => observable InvoiceSummary[]
              query FilteredInvoices => observable InvoiceSummary[]
                filter status String
                filter category Category optional
              query FilteredById => observable InvoiceSummary[]
                by invoiceId InvoiceId
                filter status String
                filter category Category optional
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceIssued key invoiceId
                  invoiceId = invoiceId
                  status = status
                  category = category
        """;

    public const string Probe = """
        using Cratis.Arc.Queries;
        using Cratis.Arc.Testing.Queries;
        using Cratis.Chronicle.ReadModels;
        using Invoices.Billing.Invoicing.Lookup;
        using Invoices.Common;
        using Microsoft.Extensions.DependencyInjection;
        using NSubstitute;
        using Xunit;

        namespace Invoices.Probes;

        public class query_filters
        {
            [Fact]
            public async Task should_apply_required_optional_and_keyed_filters()
            {
                var north = new InvoiceSummary(InvoiceId: new InvoiceId("one"), Status: "open", Category: new Category("North"));
                var closed = new InvoiceSummary(InvoiceId: new InvoiceId("two"), Status: "closed", Category: new Category("North"));
                var south = new InvoiceSummary(InvoiceId: new InvoiceId("three"), Status: "open", Category: new Category("South"));
                var readModels = Substitute.For<IReadModels>();
                readModels.GetInstances<InvoiceSummary>().Returns((IEnumerable<InvoiceSummary>)[north, closed, south]);
                readModels.Release<InvoiceSummary>(Arg.Any<InvoiceSummary>()).Returns(call => Task.FromResult(call.Arg<InvoiceSummary>()));
                using var scenario = new QueryScenario<InvoiceSummary>();
                scenario.Services.AddSingleton(readModels);
                var filtered = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), new QueryArguments { ["status"] = "open", ["category"] = "North" });
                var optional = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), new QueryArguments { ["status"] = "open" });
                var keyed = await scenario.Perform(nameof(InvoiceSummary.FilteredById), new QueryArguments { ["invoiceId"] = "three", ["status"] = "open" });
                var invalid = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), new QueryArguments { ["status"] = "open", ["category"] = "x" });
                Assert.All(new[] { filtered, optional, keyed, invalid }, result =>
                {
                    Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                    Assert.Empty(result.ValidationResults);
                });
                Assert.Same(north, Assert.Single(Assert.IsType<IEnumerable<object>>(filtered.Data, exactMatch: false)));
                Assert.Equal(2, Assert.IsType<IEnumerable<object>>(optional.Data, exactMatch: false).Count());
                Assert.Same(south, Assert.Single(Assert.IsType<IEnumerable<object>>(keyed.Data, exactMatch: false)));
                Assert.Empty(Assert.IsType<IEnumerable<object>>(invalid.Data, exactMatch: false));
                var missing = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), QueryArguments.Empty);
                Assert.NotEmpty(missing.ValidationResults);
                Assert.Null(missing.Data);
            }
        }
        """;

    public const string Specification = """
              specification FilteringInvoices
                given InvoiceIssued
                  for "one"
                  invoiceId = "one"
                  status = "open"
                  category = "North"
                given InvoiceIssued
                  for "two"
                  invoiceId = "two"
                  status = "closed"
                  category = "North"
                given InvoiceIssued
                  for "three"
                  invoiceId = "three"
                  status = "open"
                  category = "South"
                then query FilteredInvoices
                  arguments
                    status = "open"
                    category = "North"
                  result
                    invoiceId = "one"
                    status = "open"
                    category = "North"
        """;
}
