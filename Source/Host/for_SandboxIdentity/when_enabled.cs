// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_enabled : given.an_api_pipeline
{
    Task Because() => Execute();

    [Fact] void should_supply_the_persona_before_api_execution() => _context.User.IsInRole("Shopper").ShouldBeTrue();
    [Fact] void should_use_a_per_request_identity() => _context.User.ShouldNotBeSame(_persona);
}
