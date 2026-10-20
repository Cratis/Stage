// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_serving_stage_endpoints : given.an_api_pipeline
{
    Task Because() => Execute("/stage/status");

    [Fact] void should_not_add_an_identity() => _context.User.Identity!.IsAuthenticated.ShouldBeFalse();
}
