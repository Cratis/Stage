// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_using_the_cookie : given.a_started_host
{
    Task Because() => SendIdentity();
    [Fact] void should_authenticate_through_arc() => _authenticated.ShouldBeTrue();
    [Fact] void should_use_the_modeled_principal() => MappedPrincipal().UserId.ShouldEqual("alice");
    [Fact] void should_use_the_modeled_roles() => MappedPrincipal().UserRoles.ShouldEqual(["editor"]);
    [Fact] void should_use_the_modeled_claims() => MappedPrincipal().Claims.Single().val.ShouldEqual("catalog");
    [Fact] void should_set_the_id_header() => _principalId.ShouldEqual("alice");
    [Fact] void should_set_the_name_header() => _principalName.ShouldEqual("Alice Example");
}
