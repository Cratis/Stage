// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxUsers;

public class when_users_play_several_personas : Specification
{
    SandboxUsers _users = null!;

    void Because()
    {
        var result = new ScreenplayCompiler().Compile("""
            policy IsStaff
              require role "Staff"
            policy IsApprover
              require role "Approver" and claim "region" matches "north"
            persona Clerk
              policy IsStaff
            persona Manager
              policy IsStaff
              policy IsApprover
            user kari "Kari Nordmann" as Clerk, Manager
            user ola as Clerk
            user guest
            """);
        result.Diagnostics.ShouldBeEmpty();
        _users = SandboxUsers.From(result.Value!, NullLogger.Instance);
    }

    [Fact] void should_offer_the_users_in_the_order_the_model_declares_them() => _users.All.Select(user => user.Name).ToArray().ShouldEqual(["kari", "ola", "guest"]);
    [Fact] void should_present_the_name_as_the_identity() => _users.All[0].Principal.UserId.ShouldEqual("kari");
    [Fact] void should_present_the_display_name_as_the_details() => _users.All[0].Principal.UserDetails.ShouldEqual("Kari Nordmann");
    [Fact] void should_fall_back_to_the_name_without_a_display_name() => _users.All[1].Principal.UserDetails.ShouldEqual("ola");
    [Fact] void should_hold_every_role_of_every_persona_once() => _users.All[0].Principal.UserRoles.Order().ToArray().ShouldEqual(["Approver", "Staff"]);
    [Fact] void should_hold_the_claims_of_its_personas() => _users.All[0].Principal.Claims.Single(claim => claim.typ == "region").val.ShouldEqual("north");
    [Fact] void should_give_a_user_only_what_its_own_personas_give() => _users.All[1].Principal.UserRoles.ToArray().ShouldEqual(["Staff"]);
    [Fact] void should_leave_a_user_without_personas_with_no_roles() => _users.All[2].Principal.UserRoles.ToArray().ShouldBeEmpty();
    [Fact] void should_mark_the_identity_provider_as_stage() => _users.All[2].Principal.IdentityProvider.ShouldEqual(SandboxUsers.IdentityProvider);
    [Fact] void should_name_the_personas_a_user_plays() => _users.All[0].Personas.ToArray().ShouldEqual(["Clerk", "Manager"]);
}
