// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// A date-time source converts to an event source id through a culture-dependent string, which can merge distinct identities.
public class with_an_event_source_that_converts_lossily : given.an_invoice_model
{
    void Because() => Plan(invoice_model.Source("DateTime", "\"2026-01-01T00:00:00.1000000+00:00\"", "\"2026-01-02T00:00:00.0000000+00:00\"").Replace(
        FirstExpectation,
        "then InvoiceIssued\n          for \"2026-01-01T00:00:00.1000000+00:00\"\n          description = \"First payload\"",
        StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unrendered_semantic() => ErrorCodes.ShouldContain("STAGE-ESM-011");
    [Fact] void should_not_plan_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
