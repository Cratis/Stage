// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_caller_fixture : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace("when IssueInvoice\n", Caller, StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_each_caller_fixture() => _plan.Artifacts.Count(_ => _.RelativePath.EndsWith("when_issuing_first_invoice.cs", StringComparison.Ordinal) || _.RelativePath.EndsWith("when_issuing_second_invoice.cs", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_use_the_principal_override() => Artifact("when_issuing_first_invoice.cs").ShouldContain("principalOverride.BeginScope(principal)");
}
