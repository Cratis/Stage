// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_the_removal_fails : a_container
{
    ContainerRemovalResult _first = null!;
    ContainerRemovalResult _second = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
        _docker.On("rm", Failure("busy"), Success());
        await _container.Create();
    }

    async Task Because()
    {
        _first = await _container.Remove();
        _second = await _container.Remove();
    }

    [Fact] void should_report_the_first_attempt_as_failed() => _first.Outcome.ShouldEqual(ContainerRemoval.Failed);
    [Fact] void should_remove_on_the_later_attempt() => _second.Outcome.ShouldEqual(ContainerRemoval.Removed);
    [Fact] void should_inspect_again_before_each_attempt() => _docker.Count("inspect").ShouldEqual(3);
}
