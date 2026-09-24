// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// A caller fixture only means something against authorization, which the planner does not render.
public class with_a_caller_fixture : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace("when IssueInvoice\n", Caller, StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_reject_the_specifications() => ErrorCodes.ShouldContainOnly(["STAGE-ESM-011", "STAGE-ESM-011"]);
}
