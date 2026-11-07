// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxUsers;

public class when_the_provider_is_asked : Specification
{
    User[] _offered = null!;
    User[] _none = null!;

    async Task Because()
    {
        var principal = new ClientPrincipal { UserId = "kari", UserDetails = "Kari", UserRoles = ["Staff"] };
        _offered = [.. await new SandboxUsersProvider(new([new("kari", ["Clerk"], principal)])).Provide()];
        _none = [.. await new SandboxUsersProvider(SandboxUsers.None).Provide()];
    }

    [Fact] void should_offer_each_user_with_its_principal() => _offered.Single().MicrosoftIdentity.UserId.ShouldEqual("kari");
    [Fact] void should_offer_nobody_when_the_host_did_not_ask_for_users() => _none.ShouldBeEmpty();
}
