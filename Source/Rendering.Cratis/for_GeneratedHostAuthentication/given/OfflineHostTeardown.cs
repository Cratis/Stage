// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

static class OfflineHostTeardown
{
    internal static async Task Release(OfflineAuthenticationFixture fixture, Func<CancellationToken, Task> shutdown, Func<ValueTask> dispose, Action releaseResources)
    {
        var failure = (OfflineAuthenticationFailure?)null;
        try
        {
            failure = await Capture(fixture, () => Stop(shutdown));
            failure = await Capture(fixture, () => dispose().AsTask()) ?? failure;
            failure = await Capture(fixture, () => Drain(fixture, releaseResources)) ?? failure;
        }
        finally
        {
            // Disposal failure must not strand the host lease. Active requests still independently
            // prevent scenario/collection release, and their resources are untouched if draining fails.
            fixture.ReturnHost();
        }

        if (failure is not null)
        {
            throw failure;
        }
    }

    static async Task Stop(Func<CancellationToken, Task> shutdown)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await shutdown(cancellation.Token);
    }

    static async Task Drain(OfflineAuthenticationFixture fixture, Action releaseResources)
    {
        await fixture.DrainRequests().WaitAsync(TimeSpan.FromSeconds(10));
        fixture.ReleaseRequestResources(releaseResources);
    }

    static async Task<OfflineAuthenticationFailure?> Capture(OfflineAuthenticationFixture fixture, Func<Task> operation)
    {
        try
        {
            await operation();
            return null;
        }
        catch (Exception exception)
        {
            return exception as OfflineAuthenticationFailure ?? fixture.Diagnostics.Reject(exception);
        }
    }
}
