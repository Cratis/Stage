// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_qualified_names_collide_across_features : given.a_policy_application
{
    void Because() => Compile(Module("Billing", Feature("Invoicing", Slice("Issue", "Owner")), Feature("Orders", Slice("Issue", "Owner", "orderId", "OrderIssued"))));

    [Fact] void should_prefix_the_feature_path() => (ClassOf2("Invoicing"), ClassOf2("Orders")).ShouldEqual(("OwnerForInvoicingIssuePolicy", "OwnerForOrdersIssuePolicy"));

    string ClassOf2(string feature) => policy_names.ClassOf(_model, _model.Application.Modules.Single().Features.Single(candidate => candidate.Name == feature).Slices.Single().Commands.Single().Id);
}
#endif
