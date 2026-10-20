// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_serving_stage_endpoints : given.an_api_pipeline
{
    void Establish()
    {
        _context.Request.Headers["x-ms-client-principal"] = "spoofed";
        _context.Request.Headers["x-ms-client-principal-id"] = "spoofed-id";
        _context.Request.Headers["x-ms-client-principal-name"] = "spoofed-name";
    }

    Task Because() => Execute("/stage/status");

    [Fact] void should_not_add_an_identity() => _context.User.Identity!.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_remove_the_principal_header() => _context.Request.Headers.ContainsKey("x-ms-client-principal").ShouldBeFalse();
    [Fact] void should_remove_the_principal_id_header() => _context.Request.Headers.ContainsKey("x-ms-client-principal-id").ShouldBeFalse();
    [Fact] void should_remove_the_principal_name_header() => _context.Request.Headers.ContainsKey("x-ms-client-principal-name").ShouldBeFalse();
}
