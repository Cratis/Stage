// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_forwarded_headers_are_untrusted : given.an_api_pipeline
{
    void Establish()
    {
        _enabled = false;
        _context.Request.Headers["x-ms-client-principal-id"] = "forged-staff";
        _context.Request.Headers["x-ms-client-principal-name"] = "Grace";
        _context.Request.Headers["x-ms-client-principal"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            """{"identityProvider":"aad","userDetails":"Grace","userRoles":["Staff"]}"""));
    }

    Task Because() => Execute();

    [Fact] void should_leave_the_request_anonymous() => _context.User.Identity!.IsAuthenticated.ShouldBeFalse();
}
