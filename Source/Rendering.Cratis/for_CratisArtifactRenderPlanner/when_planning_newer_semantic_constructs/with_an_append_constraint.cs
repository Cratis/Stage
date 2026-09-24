// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// Chronicle enforces a constraint at append; an application without it accepts appends the reference rejects.
public class with_an_append_constraint : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "      event InvoiceIssued\n",
        "      constraint UniqueDescription\n        unique description on InvoiceIssued\n      event InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unrendered_semantic() => ErrorCodes.ShouldContain("STAGE-ESM-014");
    [Fact] void should_not_plan_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
