// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_scoped_plan_into_an_empty_destination : given.a_policy_plan
{
    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_be_compatible() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
    [Fact] void should_read_only_the_overwritten_aggregate_paths() => _readPaths.ShouldContainOnly("GeneratedPolicies/Policies.cs", "GeneratedPolicies/PolicyBodies.cs", "TypedContexts/PolicyContext.cs");
}
#endif
