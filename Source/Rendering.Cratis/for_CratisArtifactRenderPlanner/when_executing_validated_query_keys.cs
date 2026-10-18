// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_validated_query_keys(when_executing_validated_query_keys.context fixture) : IClassFixture<when_executing_validated_query_keys.context>
{
    [Fact] void should_build_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_accept_valid_keys_and_reject_out_of_rule_lookup_and_filter_keys() => fixture.Results.ShouldContain("Passed!");
    [Fact] void should_render_concept_keys_in_companion_query_calls() => Assert.All(fixture.CompanionSources, source => source.ShouldContain("new global::Invoices.Common.InvoiceId(\"invoice-one\")"));
    [Fact] void should_execute_all_three_companion_query_specifications() => fixture.CompanionOutcomes.ShouldContainOnly("Passed", "Passed", "Passed", "Passed", "Passed", "Passed", "Passed", "Passed", "Passed");

    public class context : a_generated_application
    {
        public string DebugWarnings { get; private set; } = string.Empty;
        public string Results { get; private set; } = string.Empty;
        public string[] CompanionSources { get; private set; } = [];
        public string[] CompanionOutcomes { get; private set; } = [];

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_validated_query_key_model.Source + "\n" + """
                      specification LookingUpInvoice
                        given readmodel InvoiceSummary
                          invoiceId = "invoice-one"
                          description = "North"
                        then query InvoiceById
                          arguments
                            invoiceId = "invoice-one"
                          result
                            invoiceId = "invoice-one"
                            description = "North"
                      specification LookingUpIssuedInvoice
                        given InvoiceIssued
                          for "invoice-one"
                          invoiceId = "invoice-one"
                          description = "North"
                        then query InvoiceById
                          arguments
                            invoiceId = "invoice-one"
                          result
                            invoiceId = "invoice-one"
                            description = "North"
                      specification FilteringIssuedInvoices
                        given InvoiceIssued
                          for "invoice-one"
                          invoiceId = "invoice-one"
                          description = "North"
                        then query InvoicesById
                          arguments
                            invoiceId = "invoice-one"
                          result
                            invoiceId = "invoice-one"
                            description = "North"
                """));

        async Task Because()
        {
            CompanionSources =
            [
                ReadGeneratedFile("Billing/Invoicing/Lookup/when_looking_up_invoice_is_queried.cs"),
                ReadGeneratedFile("Billing/Invoicing/Lookup/when_looking_up_issued_invoice_is_queried.cs"),
                ReadGeneratedFile("Billing/Invoicing/Lookup/when_filtering_issued_invoices_is_queried.cs")
            ];

            // SDK source generators assign event identities and register projections; a bare
            // Roslyn load cannot exercise Chronicle's discovery through the real query pipeline.
            AddGeneratedSpecification("QueryKeys.cs", a_validated_query_key_model.Probe + "\n" + """
                public class validated_keys
                {
                    [Fact]
                    public Task should_preserve_lookup_and_filtered_answers() => QueryKeys.Execute();
                }
                """);
            DebugWarnings = BuildWarnings(await Run("validated-query-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            Results = await Run("validated-query-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=query-keys.trx", "--results-directory", _evidence.FullName);
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "query-keys.trx"));
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            CompanionOutcomes = [.. results.Descendants(ns + "UnitTestResult")
                .Where(result => ((string)result.Attribute("testName")!).Contains("_is_queried", StringComparison.Ordinal))
                .Select(result => (string)result.Attribute("outcome")!)];
        }
    }
}
#endif
