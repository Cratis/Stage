// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_preserving_optional_lookup_plan_bytes : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = a_validated_query_key_model.Source.Replace("      query InvoicesById => observable InvoiceSummary[]\n        by invoiceId InvoiceId\n", string.Empty, StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_admit_the_lookup_only_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_preserve_every_planned_artifact_byte() => _plan.Digest.ShouldEqual("2f62af8da340d8f80b01a1dc75ad3ab349e86545569407ebcd782adf61f124c7");
}
