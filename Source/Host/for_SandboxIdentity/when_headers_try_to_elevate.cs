// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_headers_try_to_elevate : given.an_api_pipeline
{
    void Establish()
    {
        _context.Request.Headers["x-ms-client-principal-id"] = "forwarded-staff";
        _context.Request.Headers["x-ms-client-principal-name"] = "Grace";
        _context.Request.Headers["x-ms-client-principal"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            """{"identityProvider":"aad","userId":"forwarded-staff","userDetails":"Grace","userRoles":["Staff"]}"""));
    }

    Task Because() => Execute();

    [Fact] void should_use_the_server_selected_persona() => _context.User.FindFirstValue(ClaimTypes.NameIdentifier).ShouldEqual("persona:Shopper");
    [Fact] void should_keep_the_personas_role() => _context.User.IsInRole("Shopper").ShouldBeTrue();
    [Fact] void should_not_add_the_spoofed_role() => _context.User.IsInRole("Staff").ShouldBeFalse();
    [Fact] void should_remove_the_principal_header() => _context.Request.Headers.ContainsKey("x-ms-client-principal").ShouldBeFalse();
    [Fact] void should_remove_the_id_header() => _context.Request.Headers.ContainsKey("x-ms-client-principal-id").ShouldBeFalse();
    [Fact] void should_remove_the_name_header() => _context.Request.Headers.ContainsKey("x-ms-client-principal-name").ShouldBeFalse();
}
