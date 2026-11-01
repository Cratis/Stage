// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_sourceless_given_event : Specification
{
    string[] _errors = null!;
    int _artifacts;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource).Replace(
            "specification IssuingFirstInvoice\n",
            "specification IssuingFirstInvoice\n        given InvoiceIssued\n          description = \"Previous\"\n",
            StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        _errors = [.. plan.Diagnostics.Select(_ => _.Code)];
        _artifacts = plan.Artifacts.Length;
    }

    [Fact] void should_admit_the_unobserved_destination() => _errors.ShouldNotContain("STAGE-ESM-051");
    [Fact] void should_not_use_the_generic_refusal() => _errors.ShouldNotContain("STAGE-ESM-011");
    [Fact] void should_emit_the_artifacts() => _artifacts.ShouldBeGreaterThan(0);
}
