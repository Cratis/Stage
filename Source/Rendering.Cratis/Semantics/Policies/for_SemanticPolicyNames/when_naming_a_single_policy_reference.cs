// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_naming_a_single_policy_reference : given.a_policy_application
{
    void Because() => Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Staff"))));

    [Fact] void should_use_the_modeled_name() => NameOf("IssueInvoice").ShouldEqual("Staff");
    [Fact] void should_suffix_the_class_with_policy() => ClassOf("IssueInvoice").ShouldEqual("StaffPolicy");
}
#endif
