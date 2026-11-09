// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_the_body_claims_privileges : given.a_started_host
{
    async Task Because()
    {
        await SendSelectionBody("{\"name\":\"alice\",\"userRoles\":[\"admin\"],\"claims\":[{\"typ\":\"admin\",\"val\":\"true\"}]}");
        _response.StatusCode.ShouldEqual(HttpStatusCode.NoContent);
        await SendIdentity();
    }

    [Fact] void should_only_use_modeled_roles() => MappedPrincipal().UserRoles.Single().ShouldEqual("editor");
    [Fact] void should_only_use_modeled_claims() => MappedPrincipal().Claims.Single().typ.ShouldEqual("department");
}
