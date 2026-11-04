// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_reaping_stale_containers;

public class and_labels_change_between_listing_and_inspection : a_container
{
    ContainerReapReport _report = null!;

    void Establish()
    {
        _processes.Result = OwnerProcessState.Dead;
        var stale = new GeneratedApplicationContainerOwner { Host = "host-a", ProcessId = 1, ProcessStartedUnixSeconds = 1, RunToken = "aaaaaaaa", StartedUnixSeconds = Now.ToUnixTimeSeconds() - 600 };
        var replaced = new GeneratedApplicationContainerOwner { Host = "host-a", ProcessId = 1, ProcessStartedUnixSeconds = 1, RunToken = "cccccccc", StartedUnixSeconds = Now.ToUnixTimeSeconds() - 600 };
        _docker.On("ps", Success($"{Own}\n"));
        _docker.On("inspect", Inspected(Own, stale), Inspected(Own, replaced));
    }

    async Task Because() => _report = await _container.ReapStale();

    [Fact] void should_remove_nothing() => _report.Removed.ShouldEqual(0);
    [Fact] void should_issue_no_removal() => _docker.Count("rm").ShouldEqual(0);
}
