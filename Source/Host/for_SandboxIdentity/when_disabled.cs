// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_disabled : given.an_api_pipeline
{
    void Establish() => _enabled = false;

    Task Because() => Execute();

    [Fact] void should_leave_the_request_anonymous() => _context.User.Identity!.IsAuthenticated.ShouldBeFalse();
}
