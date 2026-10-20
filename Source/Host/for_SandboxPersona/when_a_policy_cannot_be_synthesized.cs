// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_a_policy_cannot_be_synthesized : given.a_persona_model
{
    void Because() => _principal = Create("""
        policy Impossible
          require role "Staff" and claim "owner" matches subject
        policy IsShopper
          require role "Shopper"
        persona Shopper
          policy Impossible
          policy IsShopper
        """);

    [Fact] void should_keep_the_satisfiable_policy() => _principal!.IsInRole("Shopper").ShouldBeTrue();
    [Fact] void should_skip_all_contributions_from_the_unsupported_policy() => _principal!.IsInRole("Staff").ShouldBeFalse();
    [Fact] void should_still_be_authenticated() => _principal!.Identity!.IsAuthenticated.ShouldBeTrue();
}
