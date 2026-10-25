// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_sourceless_given_event : Specification
{
    string[] _errors = null!;
    string[] _reasons = null!;
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource).Replace(
            "specification IssuingFirstInvoice\n",
            "specification IssuingFirstInvoice\n        given InvoiceIssued\n          description = \"Previous\"\n",
            StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
        _errors = [.. _plan.Diagnostics.Select(_ => _.Code)];
        _reasons = [.. _plan.Diagnostics.Select(_ => _.Message)];
    }

    [Fact] void should_reject_the_unrepresentable_null_destination() => _errors.ShouldContain("STAGE-ESM-051");
    [Fact] void should_not_use_the_generic_refusal() => _errors.ShouldNotContain("STAGE-ESM-011");
    [Fact] void should_name_the_specification_and_event() => _reasons.Any(_ => _.Contains("'IssuingFirstInvoice'", StringComparison.Ordinal) && _.Contains("'InvoiceIssued'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_the_null_destination() => _reasons.Any(_ => _.Contains("null destination", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => rendering_refusals.OmittedParts(_plan);
}
