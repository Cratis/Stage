// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc;
using Cratis.Arc.Identity;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Xunit;

namespace Cratis.Stage.Host.for_OidcPublishedHost;

public class when_arc_does_not_trust_headers : for_SemanticHttpBinding.given.a_bound_semantic_model
{
    int _status;

    protected override void ConfigureHostServices(IServiceCollection services)
    {
        services.Configure<ArcOptions>(options => options.TrustForwardedIdentityHeaders = false);
        services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
    }

    protected override void ConfigureHttpPipeline(IApplicationBuilder pipeline)
    {
        pipeline.Use((context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Staff")], "Cookies"));
            return next(context);
        });
        pipeline.UseAuthentication();
    }

    async Task Because() => (_status, _, _) = await Request("POST", "/api/projects/registration/register-project/restricted-project", Payload);

    [Fact] void should_authorize_the_existing_cookie_principal_against_the_model_policy() => _status.ShouldEqual(StatusCodes.Status200OK);
}
