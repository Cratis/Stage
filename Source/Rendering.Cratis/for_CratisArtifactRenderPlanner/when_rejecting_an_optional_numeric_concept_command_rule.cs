// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_an_optional_numeric_concept_command_rule : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = "concept Count : Int\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("        produces InvoiceIssued\n", "        count Count?\n        validate\n          count > 0\n        produces InvoiceIssued\n", StringComparison.Ordinal)
            .Replace("          streamReference = ", "          count = 2\n          streamReference = ", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_reject_the_nonnullable_unwrapped_predicate() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-005");
    [Fact] void should_emit_no_partial_application() => _plan.Artifacts.ShouldBeEmpty();
}
