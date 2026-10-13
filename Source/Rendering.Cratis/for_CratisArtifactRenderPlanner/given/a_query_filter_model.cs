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

    public const string OptionalPropertySource = """
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
                  flag = true
              event InvoiceIssued
                invoiceId InvoiceId
                status String
                category Category
                flag Bool
            slice StateView Lookup
              readmodel InvoiceSummary
                id InvoiceId key
                status String
                category Category optional
                flag Bool optional
              query AllInvoices => observable InvoiceSummary[]
              query FilteredInvoices => observable InvoiceSummary[]
                filter status String
                filter category Category optional
              query FilteredById => observable InvoiceSummary[]
                by id InvoiceId
                filter status String
                filter category Category optional
              query FilteredOptionalProperties => observable InvoiceSummary[]
                filter flag Bool
                filter category Category
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceIssued key invoiceId
                  id = invoiceId
                  status = status
                  category = category
                  flag = flag
        """;

    public static string OptionalPropertyProbe => Probe
        .Replace("new InvoiceSummary(InvoiceId:", "new InvoiceSummary(Id:", StringComparison.Ordinal)
        .Replace("[\"invoiceId\"]", "[\"id\"]", StringComparison.Ordinal)
        .Replace("Category: new Category(\"North\"))", "Category: new Category(\"North\"), Flag: true)", StringComparison.Ordinal)
        .Replace("Category: new Category(\"South\"))", "Category: new Category(\"South\"), Flag: true)", StringComparison.Ordinal) + "\n" + """
        public class optional_property_filters
        {
            [Fact]
            public async Task should_exclude_null_properties_from_required_filters()
            {
                var matching = new InvoiceSummary(Id: new InvoiceId("one"), Status: "open", Category: new Category("North"), Flag: true);
                var nullFlag = matching with { Id = new InvoiceId("two"), Flag = null };
                var nullCategory = matching with { Id = new InvoiceId("three"), Category = null };
                var differentFlag = matching with { Id = new InvoiceId("four"), Flag = false };
                using var scenario = new QueryScenario<InvoiceSummary>();
                scenario.Given.ForEventSource("one").ReadModel(matching);
                scenario.Given.ForEventSource("two").ReadModel(nullFlag);
                scenario.Given.ForEventSource("three").ReadModel(nullCategory);
                scenario.Given.ForEventSource("four").ReadModel(differentFlag);
                var result = await scenario.Perform(nameof(InvoiceSummary.FilteredOptionalProperties), new QueryArguments { ["flag"] = true, ["category"] = "North" });
                Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                Assert.Empty(result.ValidationResults);
                Assert.Same(matching, Assert.Single(Assert.IsType<IEnumerable<object>>(result.Data, exactMatch: false)));
            }
        }
        """;

    public const string OptionalPropertySpecification = """
              specification FilteringOptionalProperties
                given readmodel InvoiceSummary
                  id = "one"
                  status = "open"
                  category = "North"
                  flag = true
                given readmodel InvoiceSummary
                  id = "two"
                  status = "open"
                  category = "North"
                  flag = null
                given readmodel InvoiceSummary
                  id = "three"
                  status = "open"
                  category = null
                  flag = true
                given readmodel InvoiceSummary
                  id = "four"
                  status = "open"
                  category = "North"
                  flag = false
                then query FilteredOptionalProperties
                  arguments
                    flag = true
                    category = "North"
                  result
                    id = "one"
                    status = "open"
                    category = "North"
                    flag = true
        """;

    public const string Probe = """
        using Cratis.Arc.Chronicle.Testing.Queries;
        using Cratis.Arc.Queries;
        using Cratis.Arc.Testing.Queries;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Billing.Invoicing.Lookup;
        using Invoices.Common;
        using Xunit;

        namespace Invoices.Probes;

        public class query_filters
        {
            [Fact]
            public async Task should_apply_required_optional_and_keyed_filters()
            {
                var north = new InvoiceSummary(InvoiceId: new InvoiceId("one"), Status: "open", Category: new Category("North"));
                var south = new InvoiceSummary(InvoiceId: new InvoiceId("three"), Status: "open", Category: new Category("South"));
                using var scenario = new QueryScenario<InvoiceSummary>();
                scenario.Given.ForEventSource("one").Events(new InvoiceIssued(InvoiceId: new InvoiceId("one"), Status: "open", Category: new Category("North")));
                scenario.Given.ForEventSource("two").Events(new InvoiceIssued(InvoiceId: new InvoiceId("two"), Status: "closed", Category: new Category("North")));
                scenario.Given.ForEventSource("three").Events(new InvoiceIssued(InvoiceId: new InvoiceId("three"), Status: "open", Category: new Category("South")));
                var filtered = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), new QueryArguments { ["status"] = "open", ["category"] = "North" });
                var optional = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), new QueryArguments { ["status"] = "open" });
                var keyed = await scenario.Perform(nameof(InvoiceSummary.FilteredById), new QueryArguments { ["invoiceId"] = "three", ["status"] = "open" });
                var invalid = await scenario.Perform(nameof(InvoiceSummary.FilteredInvoices), new QueryArguments { ["status"] = "open", ["category"] = "x" });
                Assert.All(new[] { filtered, optional, keyed, invalid }, result =>
                {
                    Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                    Assert.Empty(result.ValidationResults);
                });
                Assert.Equal(north, Assert.Single(Assert.IsType<IEnumerable<object>>(filtered.Data, exactMatch: false)));
                Assert.Equal(2, Assert.IsType<IEnumerable<object>>(optional.Data, exactMatch: false).Count());
                Assert.Equal(south, Assert.Single(Assert.IsType<IEnumerable<object>>(keyed.Data, exactMatch: false)));
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
