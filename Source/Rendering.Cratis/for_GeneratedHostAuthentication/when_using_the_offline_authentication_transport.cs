// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Microsoft.Identity.Client;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_using_the_offline_authentication_transport(OfflineAuthenticationFixture fixture) : Specification
{
    [Fact]
    async Task should_retain_all_bound_clients_after_a_borrower_and_host_are_disposed()
    {
        using var scenario = fixture.BeginScenario();
        await using (var host = await OfflineHost.Start(fixture))
        {
            fixture.AssertBindings(host.Services);
            using var borrowed = ((IHttpClientFactory)fixture.Transport).CreateClient("borrowed");
            using var metadata = await borrowed.GetAsync($"{OfflineIssuer.Authority}/.well-known/openid-configuration");
            (metadata.StatusCode == HttpStatusCode.OK).ShouldBeTrue();
        }

        await fixture.AssertRetainedClients();
        (scenario.Diagnostics.Snapshot().Failures == 0).ShouldBeTrue();
    }

    [Fact]
    async Task should_serve_only_the_exact_identity_routes_and_methods_in_memory()
    {
        using var scenario = fixture.BeginScenario();
        var client = ((IMsalHttpClientFactory)fixture.Transport).GetHttpClient();
        foreach (var path in new[] { $"/{OfflineIssuer.Tenant}/v2.0/.well-known/openid-configuration", $"/{OfflineIssuer.Tenant}/v2.0/keys", "/common/discovery/instance" })
        {
            using var response = await client.GetAsync($"https://login.microsoftonline.com{path}");
            (response.StatusCode == HttpStatusCode.OK).ShouldBeTrue();
        }

        await Assert.ThrowsAsync<OfflineAuthenticationFailure>(() => client.GetAsync("https://foreign.example.test/common/discovery/instance"));
        await Assert.ThrowsAsync<OfflineAuthenticationFailure>(() => client.PostAsync($"{OfflineIssuer.Authority}/keys", null));
        var snapshot = scenario.Diagnostics.Snapshot();
        (snapshot.Metadata == 1 && snapshot.Keys == 1 && snapshot.InstanceDiscovery == 1 && snapshot.Unexpected == 2).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_the_service_fabric_native_fallback()
    {
        using var scenario = fixture.BeginScenario();
        Assert.Throws<OfflineAuthenticationFailure>(() => ((IMsalSFHttpClientFactory)fixture.Transport).GetHttpClient((_, _, _, _) => true));
        (scenario.Diagnostics.Snapshot().Unsupported == 1).ShouldBeTrue();
    }

    [Fact]
    void should_retain_safe_counts_without_recording_sensitive_exception_messages()
    {
        OfflineAuthenticationSnapshot snapshot;
        using (var scenario = fixture.BeginScenario())
        {
            for (var count = 0; count < 100; count++)
            {
                scenario.Diagnostics.Record(OfflineAuthenticationEndpoint.Metadata);
                scenario.Diagnostics.Failure(new InvalidOperationException("sensitive-sentinel"));
            }

            snapshot = scenario.Diagnostics.Snapshot();
        }

        (snapshot.Metadata == 100 && snapshot.Failures == 100).ShouldBeTrue();
        snapshot.ToString().Contains("InvalidOperationException=100", StringComparison.Ordinal).ShouldBeTrue();
        snapshot.ToString().Contains("sensitive-sentinel", StringComparison.Ordinal).ShouldBeFalse();
        using var next = fixture.BeginScenario();
        (next.Diagnostics.Snapshot().Metadata == 0 && next.Diagnostics.Snapshot().Failures == 0).ShouldBeTrue();
    }

    [Fact]
    async Task should_record_late_requests_without_reusing_a_finished_scenario()
    {
        using var scenario = fixture.BeginScenario();
        var client = ((IMsalHttpClientFactory)fixture.Transport).GetHttpClient();
        scenario.Dispose();
        Assert.Throws<OfflineAuthenticationFailure>(() => ((IMsalHttpClientFactory)fixture.Transport).GetHttpClient());
        await Assert.ThrowsAsync<OfflineAuthenticationFailure>(() => client.GetAsync($"{OfflineIssuer.Authority}/.well-known/openid-configuration"));
        var snapshot = scenario.Diagnostics.Snapshot();
        (snapshot.Unexpected == 2 && snapshot.Failures == 2).ShouldBeTrue();
        using var next = fixture.BeginScenario();
        (next.Diagnostics.Snapshot().Unexpected == 0).ShouldBeTrue();
    }

    [Fact]
    async Task should_reject_overlap_and_keep_the_lease_until_its_host_has_finished()
    {
        using var scenario = fixture.BeginScenario();
        await using var host = await OfflineHost.Start(fixture);
        Assert.Throws<OfflineAuthenticationFailure>(fixture.BeginScenario);
        Assert.Throws<OfflineAuthenticationFailure>(scenario.Dispose);
        fixture.AssertBindings(host.Services);
    }

    [Fact]
    async Task should_preserve_the_caller_owned_scenario_after_refusing_collection_teardown()
    {
        using var scenario = fixture.BeginScenario();
        Assert.Throws<OfflineAuthenticationFailure>(fixture.Dispose);
        await fixture.AssertRetainedClients();
        scenario.Dispose();
        using var next = fixture.BeginScenario();
        await fixture.AssertRetainedClients();
    }

    [Fact]
    void should_forbid_a_second_collection_from_reusing_the_process_cached_authority() =>
        Assert.Throws<OfflineAuthenticationFailure>(() => new OfflineAuthenticationFixture());
}
