// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineBrowser
{
    internal HttpStatusCode Status { get; private set; }
    internal bool SecureSession { get; private set; }
    internal string? Cookie { get; private set; }

    internal async Task Login(OfflineHost host, BrowserFailure failure = BrowserFailure.None)
    {
        try
        {
            await CompleteLogin(host, failure);
        }
        catch (Exception exception)
        {
            throw host.Diagnostics.Reject(exception);
        }
    }

    async Task CompleteLogin(OfflineHost host, BrowserFailure failure)
    {
        using var challenge = await host.Send("/", new HostRequest { Navigate = true });
        host.Diagnostics.Observe(OfflineAuthenticationPhase.ChallengeReturned);
        var query = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        var cookies = challenge.Headers.GetValues("Set-Cookie").Select(cookie => cookie.Split(';', 2)[0]);
        var nonce = query["nonce"].ToString();
        host.Issuer.Nonce = failure == BrowserFailure.Nonce ? "invalid-nonce" : nonce;
        host.Issuer.IdTokenOverride = InvalidToken(host.Issuer, failure, nonce);
        var form = new Dictionary<string, string>
        {
            ["code"] = "offline-authorization-code",
            ["state"] = failure == BrowserFailure.State ? "invalid-state" : query["state"].ToString()
        };
        using var response = await host.Callback(form, failure == BrowserFailure.Correlation ? string.Empty : string.Join("; ", cookies));
        Status = response.StatusCode;
        if (response.Headers.TryGetValues("Set-Cookie", out var sessionCookies))
        {
            var session = sessionCookies.SingleOrDefault(cookie => cookie.StartsWith("__Host-Cratis=", StringComparison.Ordinal));
            Cookie = session?.Split(';', 2)[0];
            SecureSession = session?.Contains("secure", StringComparison.OrdinalIgnoreCase) == true &&
                session.Contains("httponly", StringComparison.OrdinalIgnoreCase) && session.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase);
        }
    }

    static string? InvalidToken(OfflineIssuer issuer, BrowserFailure failure, string nonce)
    {
        var claims = new TokenClaims { Audience = OfflineIssuer.Client, Scope = false, Nonce = nonce };
        if (failure == BrowserFailure.Signature)
        {
            using var otherIssuer = new OfflineIssuer();
            return otherIssuer.Token(claims);
        }

        var invalid = InvalidClaims(claims, failure);
        return invalid is null ? null : issuer.Token(invalid);
    }

    static TokenClaims? InvalidClaims(TokenClaims claims, BrowserFailure failure) => failure switch
    {
        BrowserFailure.Issuer => claims with { Issuer = "https://foreign.example.test/v2.0" },
        BrowserFailure.Audience => claims with { Audience = "other-client" },
        BrowserFailure.Lifetime => claims with { Expired = true },
        BrowserFailure.Tenant => claims with { Tenant = "44444444-4444-4444-8444-444444444444" },
        _ => null
    };
}
