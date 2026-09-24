// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_sourceless_given_event : Specification
{
    string[] _errors = null!;
    string[] _reasons = null!;
    int _artifacts;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource).Replace(
            "specification IssuingFirstInvoice\n",
            "specification IssuingFirstInvoice\n        given InvoiceIssued\n          description = \"Previous\"\n",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        _errors = [.. plan.Diagnostics.Select(_ => _.Code)];
        _reasons = [.. plan.Diagnostics.Select(_ => _.Message)];
        _artifacts = plan.Artifacts.Length;
    }

    [Fact] void should_reject_the_unrepresentable_null_destination() => _errors.ShouldContain("STAGE-ESM-011");
    [Fact] void should_explain_the_null_destination() => _reasons.Any(_ => _.Contains("null destination", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_no_partial_artifacts() => _artifacts.ShouldEqual(0);
}
