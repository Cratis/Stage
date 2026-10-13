// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class a_protected_collection_model
{
    internal static string Source => "policy Clerks\n  require authenticated\n" + a_query_filter_model.Source
        .Replace("query FilteredInvoices => observable InvoiceSummary[]\n", "query FilteredInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal);

    internal static string Specification => a_query_filter_model.Specification
        .Replace("specification FilteringInvoices\n", "specification FilteringInvoices\n        given caller\n          authenticated\n", StringComparison.Ordinal);

    internal static string Snapshot => Source.Replace("query FilteredInvoices => observable", "query FilteredInvoices =>", StringComparison.Ordinal) + "\n" + Specification;

    const string CommandAction = """
                when IssueInvoice
                  invoiceId = "one"
                  status = "open"
                  category = "North"
                then InvoiceIssued
                  invoiceId = "one"
                  status = "open"
                  category = "North"
                then query FilteredInvoices
        """;

    internal static string CommandSnapshot => Source
        .Replace("query FilteredInvoices => observable", "query FilteredInvoices =>", StringComparison.Ordinal)
        .Replace("    slice StateView Lookup", Specification.Replace("then query FilteredInvoices", CommandAction.TrimStart(), StringComparison.Ordinal) + "\n    slice StateView Lookup", StringComparison.Ordinal);

    internal static string Routed => "eventsource InvoicesSource\n  identifier InvoiceId\n  stream Entries\n" + Source + "\n" + Specification
        .Replace("for \"one\"\n", "for \"one\"\n          stream InvoicesSource.Entries\n", StringComparison.Ordinal)
        .Replace("for \"two\"\n", "for \"two\"\n          stream InvoicesSource.Entries\n", StringComparison.Ordinal)
        .Replace("for \"three\"\n", "for \"three\"\n          stream InvoicesSource.Entries\n", StringComparison.Ordinal);

    // The Id convention supplies Arc's readable pinned key; the list also accepts a key argument.
    internal static string KeyedRouted => Routed
        .Replace("invoiceId", "id", StringComparison.Ordinal)
        .Replace("query AllInvoices", "query InvoiceById => InvoiceSummary optional\n        by id InvoiceId\n      query AllInvoices", StringComparison.Ordinal)
        .Replace("query FilteredById => observable InvoiceSummary[]\n", "query FilteredById => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal)
        .Replace("then query FilteredInvoices", "then query FilteredById", StringComparison.Ordinal)
        .Replace("arguments\n", "arguments\n            id = \"one\"\n", StringComparison.Ordinal)
        .Replace("specification FilteringInvoices", "specification ListingRoutedInvoices", StringComparison.Ordinal);

    internal static string Scoped => "type Info\n  status String\n" + Source
        .Replace("readmodel InvoiceSummary\n", "readmodel InvoiceSummary\n        info Info optional\n", StringComparison.Ordinal) + "\n" + """
                nested info
                  from InvoiceIssued key invoiceId
                    status = status
        """ + "\n" + Specification;
}
