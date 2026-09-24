// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_localized_rejection_specifications : a_generated_invoice_application
{
    bool _hostUsesRequestLocalization;

    protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
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
                ["invoices.en.strings"] = "invoice.descriptionRequired = \"Description is required\"\ninvoice.mustBeFirst = \"Must be the first payload\"\n",
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
        _hostUsesRequestLocalization = ReadGeneratedFile("Program.cs").Contains("app.UseRequestLocalization(", StringComparison.Ordinal);
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

    [Fact] void should_configure_request_culture() => _hostUsesRequestLocalization.ShouldBeTrue();
    [Fact] void should_build_without_debug_warnings() => DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_without_release_warnings() => ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_pass_all_generated_specifications() => Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_compare_both_rejection_keys() => Results.Count(_ => _.Name.Contains("should_report_the_expected_first_error_key", StringComparison.Ordinal) && _.Outcome == "Passed").ShouldEqual(2);
}
#endif
