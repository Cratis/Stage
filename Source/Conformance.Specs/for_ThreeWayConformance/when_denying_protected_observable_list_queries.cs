// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_denying_protected_observable_list_queries(when_denying_protected_observable_list_queries.context fixture) : IClassFixture<when_denying_protected_observable_list_queries.context>
{
    [Fact] void should_build_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_deny_the_unkeyed_list_before_invoking_observe() => fixture.UnkeyedOutcome.ShouldEqual("Passed");
    [Fact] void should_deny_the_filtered_list_before_validating_the_unset_required_filter() => fixture.FilteredOutcome.ShouldEqual("Passed");
    [Fact] void should_return_no_data_for_each_denial() => fixture.NoDataOutcomes.ShouldContainOnly("Passed", "Passed", "Passed");

    public class context : a_generated_application
    {
        public string Warnings { get; private set; } = string.Empty;
        public string UnkeyedOutcome { get; private set; } = string.Empty;
        public string FilteredOutcome { get; private set; } = string.Empty;
        public string[] NoDataOutcomes { get; private set; } = [];

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(Source));

        async Task Because()
        {
            // Screenplay requires the filter even for denials. Copy the generated scenario with
            // only its name and required argument changed, preserving its generated denial facts.
            var filtered = ReadGeneratedFile("Billing/Invoicing/Lookup/when_denying_filtered_list_is_queried.cs");
            filtered.ShouldContain("[\"status\"] = \"open\"");
            AddGeneratedSpecification("UnsetFilterDenial.cs", filtered
                .Replace("when_denying_filtered_list_is_queried", "when_denying_filtered_list_with_unset_argument_is_queried", StringComparison.Ordinal)
                .Replace("[\"status\"] = \"open\"", "[\"status\"] = null!", StringComparison.Ordinal));
            Warnings = BuildWarnings(await Run("denial-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            await Run("denial-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=denial.trx", "--results-directory", _evidence.FullName);
            var trx = XDocument.Load(Path.Combine(_evidence.FullName, "denial.trx"));
            var results = GeneratedTestResults.Read(trx);
            UnkeyedOutcome = Outcomes(results, className: "when_denying_unkeyed_list_is_queried", methodName: "should_be_unauthorized").Single();
            FilteredOutcome = Outcomes(results, className: "when_denying_filtered_list_with_unset_argument_is_queried", methodName: "should_be_unauthorized").Single();
            NoDataOutcomes = Outcomes(results, methodName: "should_return_no_data");
        }

        static string[] Outcomes(IEnumerable<GeneratedTestResult> results, string? className = null, string? methodName = null) =>
            [.. results.Where(result => result.Matches(className: className, methodName: methodName)).Select(result => result.Outcome)];

        static string Source => "policy Clerks\n  require authenticated\n" + a_query_filter_model.Source
            .Replace("query AllInvoices => observable InvoiceSummary[]\n", "query AllInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal)
            .Replace("query FilteredInvoices => observable InvoiceSummary[]\n", "query FilteredInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal) + "\n" + """
                  specification DenyingUnkeyedList
                    given caller
                    then query AllInvoices
                    then denied
                  specification DenyingFilteredList
                    given caller
                    then query FilteredInvoices
                      arguments
                        status = "open"
                        category = "North"
                    then denied
            """;
    }
}
#endif
