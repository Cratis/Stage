// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_protected_observable_list_specifications : a_generated_application
{
    string _build = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = "policy Clerks\n  require authenticated\n" + a_query_filter_model.Source
            .Replace("query FilteredInvoices => observable InvoiceSummary[]\n", "query FilteredInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal) + "\n" + a_query_filter_model.Specification
            .Replace("specification FilteringInvoices\n", "specification FilteringInvoices\n        given caller\n          authenticated\n", StringComparison.Ordinal);
        var unfiltered = a_query_filter_model.Specification
            .Replace("specification FilteringInvoices\n", "specification ListingInvoices\n        given caller\n          authenticated\n", StringComparison.Ordinal)
            .Replace("then query FilteredInvoices\n          arguments\n            status = \"open\"\n            category = \"North\"", "then query AllInvoices", StringComparison.Ordinal) + "\n          result\n            invoiceId = \"two\"\n            status = \"closed\"\n            category = \"North\"\n          result\n            invoiceId = \"three\"\n            status = \"open\"\n            category = \"South\"";
        source = source.Replace("query AllInvoices => observable InvoiceSummary[]\n", "query AllInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal) + "\n" + unfiltered;
        return invoice_model.Plan(invoice_model.Compile(source));
    }

    async Task Because()
    {
        _build = await Run("protected-list-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _tests = await Run("protected-list-tests.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact] void should_build_without_warnings() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_pass_filtered_and_unfiltered_row_expectations() => _tests.ShouldHavePassedSpecifications();
    [Fact] void should_use_the_default_query_scenario_store() => ReadGeneratedFile("Billing/Invoicing/Lookup/when_filtering_invoices_is_queried.cs").ShouldNotContain("Substitute");
}
#endif
