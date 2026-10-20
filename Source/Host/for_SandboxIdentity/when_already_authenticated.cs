// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_already_authenticated : given.an_api_pipeline
{
    void Establish() => _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Actual caller"), new Claim(ClaimTypes.Role, "Staff")], "real-auth"));

    Task Because() => Execute();

    [Fact] void should_replace_the_existing_identity() => _context.User.Identity!.Name.ShouldEqual("Ada");
    [Fact] void should_remove_incoming_privileges() => _context.User.IsInRole("Staff").ShouldBeFalse();
}
