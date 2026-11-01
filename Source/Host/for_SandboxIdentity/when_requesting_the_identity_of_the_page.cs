// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_requesting_the_identity_of_the_page : given.an_api_pipeline
{
    Task Because() => Execute("/.cratis/me");

    [Fact] void should_authenticate_as_the_persona_being_played() => _context.User.IsInRole("Shopper").ShouldBeTrue();

    // Arc's `/.cratis/me` answers 401 exactly when the user is not authenticated.
    [Fact] void should_be_authenticated() => _context.User.Identity.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_use_a_per_request_identity() => _context.User.ShouldNotBeSame(_persona);
}
