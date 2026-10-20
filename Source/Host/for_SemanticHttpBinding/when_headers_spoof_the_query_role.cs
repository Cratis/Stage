// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding;

public class when_headers_spoof_the_query_role : given.a_persona_bound_query
{
    protected override string PersonaName => "Staff";
    protected override string PersonaPolicy => "IsStaff";

    protected override void ConfigureHttpPipeline(IApplicationBuilder pipeline)
    {
        pipeline.Use((context, next) =>
        {
            context.Request.Headers["x-ms-client-principal-id"] = "shopper";
            context.Request.Headers["x-ms-client-principal-name"] = "Spoofed shopper";
            context.Request.Headers["x-ms-client-principal"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                """{"identityProvider":"aad","userId":"shopper","userDetails":"Spoofed shopper","userRoles":["Shopper"]}"""));
            return next(context);
        });
        base.ConfigureHttpPipeline(pipeline);
    }

    async Task Because() => (_status, _, _) = await Request("GET", "/api/projects/lookup/project-lookup/projects");

    [Fact] void should_not_promote_the_persona_using_spoofed_headers() => _status.ShouldEqual(StatusCodes.Status403Forbidden);
}
