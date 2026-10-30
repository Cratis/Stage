// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_operations_bind_a_policy_differently : given.a_policy_application
{
    void Because() => Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Owner"), Slice("VoidInvoice", "Owner", "orderId"))));

    [Fact] void should_qualify_every_operation_with_its_name() => (ClassOf("IssueInvoice"), ClassOf("VoidInvoice")).ShouldEqual(("OwnerForIssueInvoicePolicy", "OwnerForVoidInvoicePolicy"));
    [Fact] void should_register_the_qualified_policy_name() => NameOf("IssueInvoice").ShouldEqual("OwnerForIssueInvoice");
    [Fact] void should_not_use_a_hash() => new[] { ClassOf("IssueInvoice"), ClassOf("VoidInvoice") }.All(name => name.All(char.IsLetter)).ShouldBeTrue();
}
#endif
