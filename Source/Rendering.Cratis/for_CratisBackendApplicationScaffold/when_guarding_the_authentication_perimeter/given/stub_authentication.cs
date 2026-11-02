// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter.given;

public class stub_authentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authenticated = Scheme.Name == the_emitted_perimeter.Cookie
            ? Request.Cookies["perimeter-session"] == "synthetic-session"
            : Scheme.Name == the_emitted_perimeter.Bearer && Request.Headers.Authorization == "Bearer <synthetic-token>";
        var result = authenticated
            ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "synthetic-caller")], Scheme.Name)), Scheme.Name))
            : AuthenticateResult.NoResult();
        return Task.FromResult(result);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = Scheme.Name == the_emitted_perimeter.OpenIdConnect ? StatusCodes.Status302Found : StatusCodes.Status401Unauthorized;
        Response.Headers["Challenged-Scheme"] = Scheme.Name;
        if (Scheme.Name == the_emitted_perimeter.OpenIdConnect)
        {
            Response.Headers.Location = "https://signin.invalid/authorize";
            Response.Headers["Return-Url"] = properties.RedirectUri;
        }
        return Task.CompletedTask;
    }
}
