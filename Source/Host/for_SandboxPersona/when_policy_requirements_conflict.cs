// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_policy_requirements_conflict : given.a_persona_model
{
    void Because() => _principal = Create("""
        policy IsStaff
          require role "Staff"
        policy IsNotStaff
          require not role "Staff"
        persona Staff
          policy IsStaff
          policy IsNotStaff
        """);

    [Fact] void should_keep_the_roles_from_the_satisfiable_policy() => _principal!.IsInRole("Staff").ShouldBeTrue();
    [Fact] void should_remain_authenticated() => _principal!.Identity!.IsAuthenticated.ShouldBeTrue();
}
