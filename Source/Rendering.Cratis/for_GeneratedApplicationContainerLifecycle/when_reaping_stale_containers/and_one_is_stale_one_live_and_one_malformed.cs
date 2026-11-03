// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_reaping_stale_containers;

public class and_one_is_stale_one_live_and_one_malformed : a_container
{
    ContainerReapReport _report = null!;

    void Establish()
    {
        _processes.Result = OwnerProcessState.Dead;
        _docker.On("ps", Success($"{Own}\n{Other}\n{Third}\n"));
        var stale = new GeneratedApplicationContainerOwner { Host = "host-a", ProcessId = 1, ProcessStartedUnixSeconds = 1, RunToken = "aaaaaaaa", StartedUnixSeconds = Now.ToUnixTimeSeconds() - 600 };
        var live = new GeneratedApplicationContainerOwner { Host = "host-b", ProcessId = 1, ProcessStartedUnixSeconds = 1, RunToken = "bbbbbbbb", StartedUnixSeconds = Now.ToUnixTimeSeconds() - 600 };
        _docker.On(arguments => arguments[0] == "inspect" && arguments[^1] == Own.Value, null, Inspected(Own, stale));
        _docker.On(arguments => arguments[0] == "inspect" && arguments[^1] == Other.Value, null, Inspected(Other, live));
        _docker.On(arguments => arguments[0] == "inspect" && arguments[^1] == Third.Value, null, Success($"{Third}\n{{\"io.cratis.stage.harness.fixture\":\"standalone-generated-application\"}}\n"));
        _docker.On("rm", Success());
    }

    async Task Because() => _report = await _container.ReapStale();

    [Fact] void should_list_only_by_the_fixture_family_label() => Commands("ps").Single().ShouldEqual("ps --all --no-trunc --quiet --filter label=io.cratis.stage.harness.fixture=standalone-generated-application");
    [Fact] void should_remove_only_the_stale_container_by_identifier() => Commands("rm").Single().ShouldEqual($"rm --force --volumes {Own}");
    [Fact] void should_inspect_the_stale_container_again_before_removing_it() => Commands("inspect").Count(command => command.EndsWith(Own.Value)).ShouldEqual(2);
    [Fact] void should_count_one_removal() => _report.Removed.ShouldEqual(1);
    [Fact] void should_report_every_examined_container() => _report.Lines.Count.ShouldEqual(3);
}
