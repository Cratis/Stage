// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_selecting_the_user_menu_persona : given.a_persona_model
{
    void Because() => _principal = Create(Personas + "\n" + Menu);

    [Fact] void should_use_the_named_persona_instead_of_the_first() => _principal!.FindFirstValue(ClaimTypes.NameIdentifier).ShouldEqual("persona:Shopper");
    [Fact] void should_use_the_menu_display_name() => _principal!.Identity!.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_be_authenticated() => _principal!.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_carry_the_policy_role() => _principal!.IsInRole("Shopper").ShouldBeTrue();
    [Fact] void should_carry_the_policy_claim() => _principal!.FindFirstValue("department").ShouldEqual("shopping");
    [Fact] void should_not_grant_another_personas_role() => _principal!.IsInRole("Staff").ShouldBeFalse();
}
