// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_it_is_called_twice : a_container
{
    ContainerRemovalResult _second = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
        _docker.On("rm", Success());
        await _container.Create();
    }

    async Task Because()
    {
        await _container.Remove();
        _second = await _container.Remove();
    }

    [Fact] void should_do_nothing_the_second_time() => _second.Outcome.ShouldEqual(ContainerRemoval.NotCreated);
    [Fact] void should_remove_only_once() => _docker.Count("rm").ShouldEqual(1);
}
