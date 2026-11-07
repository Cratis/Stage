// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_the_process_exits;

public class and_the_container_is_destroyed_afterwards : a_container
{
    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
        _docker.On("rm", Success());
        await _container.Create();
    }

    async Task Because()
    {
        _container.RemoveWhenExiting();
        await _container.Remove();
    }

    [Fact] void should_remove_exactly_once_by_identifier() => Commands("rm").Single().ShouldEqual($"rm --force --volumes {Own}");
}
