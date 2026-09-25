// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_localized_concept_rejection : a_generated_invoice_application
{
    protected override string InvoiceSource => "concept Description : String\n  validate\n    not empty message $strings.invoice.descriptionRequired\n" +
        invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("description String", "description Description", StringComparison.Ordinal)
            .Replace("description = \"Second payload\"\n          streamReference", "description = \"\"\n          streamReference", StringComparison.Ordinal)
            .Replace("then InvoiceIssued\n          description = \"Second payload\"", "then error \"$strings.invoice.descriptionRequired\"", StringComparison.Ordinal);

    protected override ArtifactRenderPlan CreatePlan()
    {
        var model = invoice_model.Compile(InvoiceSource);
        var profile = CratisRendering.CreateProfile(
            model.Application.Name,
            new("InvoiceApp", "Invoices"),
            null,
            new Dictionary<string, string> { ["invoices.en.strings"] = "invoice.descriptionRequired = \"Description is required\"\n" },
            "en");
        return new CratisArtifactRenderPlanner().Plan(new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            profile,
            new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }

    async Task Because() => await VerifyGeneratedApplication();

    [Fact] void should_pass_generated_concept_key_specification() => Results.Any(_ => _.Name.Contains("should_report_the_expected_first_error_key", StringComparison.Ordinal) && _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_build_debug_without_warnings() => DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => ReleaseWarnings.ShouldBeEmpty();
}
#endif
