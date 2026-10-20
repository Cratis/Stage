// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_no_persona_exists : given.an_api_pipeline
{
    void Establish()
    {
        _persona = null;
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Staff")], "incoming"));
    }

    Task Because() => Execute();

    [Fact] void should_discard_the_incoming_identity() => _context.User.Identity!.IsAuthenticated.ShouldBeFalse();
}
