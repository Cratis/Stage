// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_authorized_command : given.an_invoice_model
{
    void Because() => Plan(Policy + Invoices
        .Replace("        produces InvoiceIssued\n", "        authorize Clerks\n        produces InvoiceIssued\n", StringComparison.Ordinal)
        .Replace("when IssueInvoice\n", Caller, StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_register_the_arc_policy_for_the_scenario() => Artifact("when_issuing_first_invoice.cs").ShouldContain("GeneratedPolicies.Registration.Register(_scenario.Services)");
    [Fact] void should_execute_as_the_declared_caller() => Artifact("when_issuing_first_invoice.cs").ShouldContain("principalOverride.BeginScope(principal)");
}
