// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_disabled_with_an_existing_principal : given.an_api_pipeline
{
    void Establish()
    {
        _enabled = false;
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Actual caller")], "real-auth"));
    }

    Task Because() => Execute();

    [Fact] void should_leave_the_existing_identity_alone() => _context.User.Identity!.Name.ShouldEqual("Actual caller");
}
