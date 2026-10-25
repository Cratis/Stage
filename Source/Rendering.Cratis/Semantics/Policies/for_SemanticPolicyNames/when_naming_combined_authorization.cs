// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_naming_combined_authorization : given.a_policy_application
{
    void Because() => Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Staff and Admin"), Slice("VoidInvoice", "Staff or Admin"))));

    [Fact] void should_join_and_operands() => ClassOf("IssueInvoice").ShouldEqual("StaffAndAdminPolicy");
    [Fact] void should_join_or_operands() => ClassOf("VoidInvoice").ShouldEqual("StaffOrAdminPolicy");
}
#endif
