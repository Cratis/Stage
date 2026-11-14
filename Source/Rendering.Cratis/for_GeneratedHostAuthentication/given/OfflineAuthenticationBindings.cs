// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineAuthenticationBindings(OfflineAuthenticationFixture fixture)
{
    internal void Assert(IServiceProvider services)
    {
        Require(ReferenceEquals(services.GetRequiredService<IHttpClientFactory>(), fixture.Transport));
        Require(ReferenceEquals(services.GetRequiredService<IMsalHttpClientFactory>(), fixture.Transport));
        Require(ReferenceEquals(services.GetRequiredService<IMsalMtlsHttpClientFactory>(), fixture.Transport));
        Require(ReferenceEquals(services.GetRequiredService<IMsalSFHttpClientFactory>(), fixture.Transport));
        var issuerOptions = services.GetRequiredService<IOptions<AadIssuerValidatorOptions>>().Value;
        Require(issuerOptions.HttpClientName == OfflineIssuer.IssuerValidationClientName);
        var oidc = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OfflineHost.OidcScheme);
        Require(fixture.Transport.Owns(oidc.Backchannel));
        var identity = services.GetRequiredService<IOptionsMonitor<MicrosoftIdentityOptions>>().Get(OfflineHost.OidcScheme);
        var credentials = identity.ClientCredentials?.ToArray() ?? [];
        Require(credentials is [{ SourceType: CredentialSource.Certificate }]);
        Require(ReferenceEquals(credentials[0].Certificate, fixture.Issuer.Certificate));
    }

    internal async Task AssertRetainedClients()
    {
        var issuer = ((IHttpClientFactory)fixture.Transport).CreateClient(OfflineIssuer.IssuerValidationClientName);
        var msal = ((IMsalHttpClientFactory)fixture.Transport).GetHttpClient();
        var certificate = ((IMsalMtlsHttpClientFactory)fixture.Transport).GetHttpClient(fixture.Issuer.Certificate);
        Require(ReferenceEquals(msal, certificate));
        Require(fixture.Transport.Owns(issuer));
        Require(fixture.Transport.Owns(msal));
        foreach (var client in new[] { issuer, msal, certificate })
        {
            await AssertMetadata(client);
        }
    }

    async Task AssertMetadata(HttpClient client)
    {
        try
        {
            using var response = await client.GetAsync($"{OfflineIssuer.Authority}/.well-known/openid-configuration");
            Require(response.IsSuccessStatusCode);
        }
        catch (Exception exception)
        {
            throw fixture.Diagnostics.Reject(exception);
        }
    }

    void Require(bool matches)
    {
        if (!matches)
        {
            throw fixture.Diagnostics.Reject(new InvalidOperationException());
        }
    }
}
