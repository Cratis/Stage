// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication;

public class RecordingAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "recording";

    public static List<string> SeenIdentityHeaders { get; } = [];

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        SeenIdentityHeaders.AddRange(Request.Headers.Keys.Where(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase)));
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
