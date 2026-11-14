// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_releasing_a_failed_offline_host(OfflineAuthenticationFixture fixture) : Specification
{
    readonly OfflineAuthenticationFixture _fixture = fixture;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_drain_requests_and_balance_the_lease_even_when_disposal_fails(bool shutdownFails)
    {
        using var scenario = _fixture.BeginScenario();
        _fixture.BorrowHost();
        _fixture.BeginRequest();
        var shutdownAttempted = false;
        var resourcesReleased = false;
        var borrowedFixture = _fixture;
        var teardown = OfflineHostTeardown.Release(
            borrowedFixture,
            _ =>
            {
                shutdownAttempted = true;
                return shutdownFails ? Task.FromException(new InvalidOperationException("sensitive-sentinel")) : Task.CompletedTask;
            },
            () => ValueTask.FromException(new IOException("sensitive-sentinel")),
            () => resourcesReleased = true);
        var waitedForRequests = !teardown.IsCompleted && !resourcesReleased;
        _fixture.EndRequest();
        var failure = await Assert.ThrowsAsync<OfflineAuthenticationFailure>(() => teardown);

        shutdownAttempted.ShouldBeTrue();
        waitedForRequests.ShouldBeTrue();
        resourcesReleased.ShouldBeTrue();
        (failure.InnerException is null).ShouldBeTrue();
        failure.Message.Contains("sensitive-sentinel", StringComparison.Ordinal).ShouldBeFalse();
        (scenario.Diagnostics.Snapshot().Failures == (shutdownFails ? 2 : 1)).ShouldBeTrue();
        scenario.Dispose();
        using var next = _fixture.BeginScenario();
        await _fixture.AssertRetainedClients();
    }
}
