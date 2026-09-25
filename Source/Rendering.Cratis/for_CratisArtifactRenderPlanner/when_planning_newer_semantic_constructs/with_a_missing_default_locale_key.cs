// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_missing_default_locale_key : given.an_invoice_model
{
    void Because()
    {
        _model = invoice_model.Compile(Invoices.Replace(
            "        produces InvoiceIssued\n",
            "        validate\n          description not empty message $strings.invoice.missing\n        produces InvoiceIssued\n",
            StringComparison.Ordinal));
        var profile = CratisRendering.CreateProfile(
            _model.Application.Name,
            new("InvoiceApp", "Invoices"),
            null,
            new Dictionary<string, string> { ["invoices.en.strings"] = "invoice.other = \"Other\"\n" },
            "en");
        _plan = new CratisArtifactRenderPlanner().Plan(new(
            _model,
            SemanticExecutionPlan.Compile(_model).Plan!,
            profile,
            new(ArtifactRenderScopeKind.Application, _model.Application.Id)));
    }

    [Fact] void should_fail_without_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_missing_default_key() => ErrorCodes.ShouldContainOnly(["STAGE-ESM-018"]);
}
