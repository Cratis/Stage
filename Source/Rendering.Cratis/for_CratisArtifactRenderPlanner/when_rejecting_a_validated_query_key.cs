// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_a_validated_query_key : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = when_rendering_portable_authorization.Source.Replace(
            "concept InvoiceId : String", "concept InvoiceId : String\n  validate\n    min 2", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_reject_arc_validation_of_the_query_argument() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-010");
    [Fact] void should_emit_no_partial_application() => _plan.Artifacts.ShouldBeEmpty();
}
