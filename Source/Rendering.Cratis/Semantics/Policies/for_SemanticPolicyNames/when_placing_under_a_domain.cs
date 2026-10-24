// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_placing_under_a_domain : given.a_policy_application
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Staff"))));
        var scene = new CratisPlanOptions("InvoiceModel", "InvoiceApp", "Invoices") { Domain = "Sales/Retail" };
        var loaded = opaque_policy_model.Load(Policies + "\n" + Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Staff"))));
        _plan = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Module("Billing")]), scene).Plan!;
    }

    [Fact] void should_prefix_the_policy_name_with_the_domain() => System.Text.Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Sales/Retail/GeneratedPolicies/StaffPolicy.cs").Bytes.AsSpan()).ShouldContain("\"Sales.Retail.Staff\"");
}
#endif
