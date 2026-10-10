// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_scoped_plan_over_the_aggregate_layout : given.a_policy_plan
{
    void Establish() => _existing["GeneratedPolicies/Policies.cs"] = LegacyPolicies;
    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_require_application_scope() => _check.ShouldBeOfExactType<CratisPublicationCheck.RequiresApplicationScope>();
    [Fact] void should_name_the_overwritten_aggregate() => ((CratisPublicationCheck.RequiresApplicationScope)_check).Paths.ShouldContainOnly("GeneratedPolicies/Policies.cs");
    [Fact] void should_explain_why_application_scope_is_required() => ((CratisPublicationCheck.RequiresApplicationScope)_check).Reason.ShouldNotBeEmpty();
}
#endif
