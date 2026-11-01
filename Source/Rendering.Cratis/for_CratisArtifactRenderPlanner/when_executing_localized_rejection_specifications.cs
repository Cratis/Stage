// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_localized_rejection_specifications(when_executing_localized_rejection_specifications.context fixture) : IClassFixture<when_executing_localized_rejection_specifications.context>
{
    [Fact] void should_configure_request_culture() => fixture.HostUsesRequestLocalization.ShouldBeTrue();
    [Fact]
    void should_use_the_short_resolver_across_nested_namespaces()
    {
        fixture.ValidatorContent.ShouldContain("GeneratedStrings.Resolve(\"$strings.invoice.descriptionRequired\")");
        fixture.ValidatorContent.ShouldNotContain("global::Invoices.GeneratedStrings");
    }
    [Fact] void should_escape_line_separators_in_generated_catalog() => fixture.CatalogEscapesSeparators.ShouldBeTrue();
    [Fact] void should_build_without_debug_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_without_release_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_pass_all_generated_specifications() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_compare_both_rejection_keys() => fixture.Results.Count(_ => _.Matches(methodName: "should_report_the_expected_first_error_key") && _.Outcome == "Passed").ShouldEqual(2);

    public class context : a_generated_invoice_application
    {
        public bool HostUsesRequestLocalization { get; private set; }
        public string ValidatorContent { get; private set; } = string.Empty;
        public bool CatalogEscapesSeparators { get; private set; }

        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("module Billing", "module Invoices", StringComparison.Ordinal)
            .Replace(
                "        produces InvoiceIssued\n",
                "        validate\n          description not empty message $strings.invoice.descriptionRequired\n          require description == \"First payload\"\n            message $strings.invoice.mustBeFirst\n        produces InvoiceIssued\n",
                StringComparison.Ordinal)
            .Replace("description = \"Second payload\"\n          streamReference", "description = \"\"\n          streamReference", StringComparison.Ordinal)
            .Replace("then InvoiceIssued\n          description = \"Second payload\"", "then error \"$strings.invoice.descriptionRequired\"", StringComparison.Ordinal)
            + "\n      specification RejectingDifferentDescription\n        when IssueInvoice\n          description = \"Other payload\"\n          streamReference = \"invoice-three\"\n        then error \"$strings.invoice.mustBeFirst\"\n";

        protected override ArtifactRenderPlan CreatePlan()
        {
            var model = invoice_model.Compile(InvoiceSource);
            var profile = CratisRendering.CreateProfile(
                model.Application.Name,
                new("InvoiceApp", "Invoices"),
                null,
                new Dictionary<string, string>
                {
                    ["invoices.en.strings"] = "invoice.descriptionRequired = \"Description is required\"\ninvoice.mustBeFirst = \"Must be the first payload\"\ninvoice.B = \"Upper\"\ninvoice.a = \"Lower\"\ninvoice_c = \"Underscore\"\ninvoice.separator = \"First\u2028Second\u2029Third\"\n",
                    ["invoices.nb.strings"] = "invoice.descriptionRequired = \"Beskrivelse kreves\"\n"
                },
                "en");
            return new CratisArtifactRenderPlanner().Plan(new(
                model,
                SemanticExecutionPlan.Compile(model).Plan!,
                profile,
                new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        }

        async Task Because()
        {
            HostUsesRequestLocalization = ReadGeneratedFile("Program.cs").Contains("app.UseRequestLocalization(", StringComparison.Ordinal);
            ValidatorContent = string.Join('\n', CreatePlan().Artifacts
                .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))
                .Where(content => content.Contains("GeneratedStrings.Resolve(", StringComparison.Ordinal)));
            CatalogEscapesSeparators = ReadGeneratedFile("GeneratedStrings.cs").Contains("First\\u2028Second\\u2029Third", StringComparison.Ordinal);
            AddGeneratedSpecification("GeneratedStringsSpecifications.cs", """
                // Copyright (c) Cratis. All rights reserved.
                // Licensed under the MIT license. See LICENSE file in the project root for full license information.

                #if DEBUG
                using System.Globalization;
                using Xunit;

                namespace Invoices;

                public class GeneratedStringsSpecifications
                {
                    [Fact]
                    public void ResolvesActiveCultureAndFallsBack()
                    {
                        var previous = CultureInfo.CurrentUICulture;
                        try
                        {
                            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
                            Assert.Equal("Beskrivelse kreves", GeneratedStrings.Resolve("$strings.invoice.descriptionRequired"));
                            Assert.Equal("Must be the first payload", GeneratedStrings.Resolve("$strings.invoice.mustBeFirst"));
                            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                            Assert.Equal("Description is required", GeneratedStrings.Resolve("$strings.invoice.descriptionRequired"));
                            Assert.Equal("First\u2028Second\u2029Third", GeneratedStrings.Resolve("$strings.invoice.separator"));
                        }
                        finally
                        {
                            CultureInfo.CurrentUICulture = previous;
                        }
                    }
                }
                #endif
                """);
            await VerifyGeneratedApplication();
        }
    }
}
#endif
