// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_operations_share_an_effective_policy : given.a_policy_application
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Staff"), Slice("VoidInvoice", "Staff"))));
        _plan = invoice_model.Plan(_model);
    }

    [Fact] void should_give_both_the_plain_name() => (ClassOf("IssueInvoice"), ClassOf("VoidInvoice")).ShouldEqual(("StaffPolicy", "StaffPolicy"));
    [Fact] void should_share_one_file() => _plan.Artifacts.Count(artifact => artifact.RelativePath == "GeneratedPolicies/StaffPolicy.cs").ShouldEqual(1);
    [Fact] void should_list_both_operations_as_sources() => _plan.Artifacts.Single(artifact => artifact.RelativePath == "GeneratedPolicies/StaffPolicy.cs").Sources.Length.ShouldEqual(2);
}
#endif
