// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_unrecognized_content : given.a_policy_plan
{
    void Establish() => _existing["GeneratedPolicies/Policies.cs"] = "unreadable content that is not a policy registry";
    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_refuse_without_throwing() => _check.ShouldBeOfExactType<CratisPublicationCheck.RequiresApplicationScope>();
}
#endif
