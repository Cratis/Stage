// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// Tags travel with every produced fact; an application that drops them appends different events.
public class with_production_tags : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "          for streamReference\n",
        "          for streamReference\n          tag \"billing\"\n",
        StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unrendered_semantic() => ErrorCodes.ShouldContain("STAGE-ESM-006");
    [Fact] void should_not_plan_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
