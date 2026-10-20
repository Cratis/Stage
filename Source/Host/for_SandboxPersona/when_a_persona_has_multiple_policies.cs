// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_a_persona_has_multiple_policies : given.a_persona_model
{
    void Because() => _principal = Create("""
        policy IsStaff
          require role "Staff"
        policy IsMember
          require role "Member" and claim "region" matches "north"
        persona StaffMember
          policy IsStaff
          policy IsMember
        """);

    [Fact] void should_collect_the_first_role() => _principal!.IsInRole("Staff").ShouldBeTrue();
    [Fact] void should_collect_the_second_role() => _principal!.IsInRole("Member").ShouldBeTrue();
    [Fact] void should_collect_claims_from_the_other_policy() => _principal!.FindFirst("region")!.Value.ShouldEqual("north");
}
