// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_more_expected_events_than_the_command_produces : an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "then InvoiceIssued\n          description = \"First payload\"",
        "then InvoiceIssued\n          description = \"First payload\"\n        then InvoiceIssued\n          description = \"First payload\"",
        StringComparison.Ordinal));

    [Fact] void should_reject_the_impossible_event_count() => ErrorCodes.ShouldContain("STAGE-ESM-011");
    [Fact] void should_not_emit_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
