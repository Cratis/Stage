// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_validating_bearer_tokens(OfflineAuthenticationFixture fixture) : Specification
{
    readonly List<HttpStatusCode> _accepted = [];
    readonly List<HttpStatusCode> _rejected = [];
    HttpStatusCode _metadataAfterDisposal;
    HttpStatusCode _firstHost;

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        var cachedMetadata = ((IHttpClientFactory)fixture.Transport).CreateClient(OfflineIssuer.IssuerValidationClientName);
        await using (var first = await OfflineHost.Start(fixture))
        {
            using var response = await first.Send("/api/query", new HostRequest { Authorization = $"Bearer {first.Issuer.Token()}" });
            _firstHost = response.StatusCode;
        }

        // The authority cache can retain this exact client after its original host has been disposed.
        using (var metadata = await cachedMetadata.GetAsync($"{OfflineIssuer.Authority}/.well-known/openid-configuration"))
        {
            _metadataAfterDisposal = metadata.StatusCode;
        }

        await using var host = await OfflineHost.Start(fixture);
        using var otherIssuer = new OfflineIssuer();
        foreach (var token in new[] { host.Issuer.Token(), host.Issuer.Token(new TokenClaims { Scope = false, AppRole = true }) })
        {
            using var response = await host.Send("/api/command", new HostRequest { Method = "POST", Authorization = $"Bearer {token}" });
            _accepted.Add(response.StatusCode);
        }

        string[] invalid =
        [
            host.Issuer.Token(new TokenClaims { Expired = true }),
            host.Issuer.Token(new TokenClaims { Audience = "api://other-host" }),
            host.Issuer.Token(new TokenClaims { Issuer = "https://foreign.example.test/v2.0" }),
            host.Issuer.Token(new TokenClaims { Tenant = "44444444-4444-4444-8444-444444444444" }),
            host.Issuer.Token(new TokenClaims { Scope = false }),
            otherIssuer.Token()
        ];
        foreach (var token in invalid)
        {
            using var response = await host.Send("/api/query", new HostRequest { Cookie = host.SessionCookie(), Authorization = $"Bearer {token}" });
            _rejected.Add(response.StatusCode);
        }
    }

    [Fact] void should_keep_cached_issuer_metadata_alive_across_hosts() => (_firstHost == HttpStatusCode.OK && _metadataAfterDisposal == HttpStatusCode.OK).ShouldBeTrue();
    [Fact] void should_accept_delegated_and_application_tokens_without_origin() => _accepted.ShouldEqual(Enumerable.Repeat(HttpStatusCode.OK, 2));
    [Fact] void should_enforce_signature_issuer_audience_lifetime_tenant_and_permissions() => _rejected.ShouldEqual(Enumerable.Repeat(HttpStatusCode.Unauthorized, 6));
}
