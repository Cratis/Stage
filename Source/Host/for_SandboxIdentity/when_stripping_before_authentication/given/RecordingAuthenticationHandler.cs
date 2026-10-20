// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication.given;

public class RecordingAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "recording";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var recording = Context.RequestServices.GetRequiredService<AuthenticationRecording>();
        recording.Invocations.Enqueue([.. Request.Headers.Keys.Where(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase))]);
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
