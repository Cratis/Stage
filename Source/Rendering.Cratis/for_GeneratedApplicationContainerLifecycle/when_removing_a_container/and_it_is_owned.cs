// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_it_is_owned : a_container
{
    ContainerRemovalResult _result = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
        _docker.On("rm", Success());
        await _container.Create();
    }

    async Task Because() => _result = await _container.Remove();

    [Fact] void should_remove_it() => _result.Outcome.ShouldEqual(ContainerRemoval.Removed);
    [Fact] void should_remove_exactly_once_by_identifier() => Commands("rm").Single().ShouldEqual($"rm --force --volumes {Own}");
    [Fact] void should_inspect_immediately_before_removing() => _docker.Calls[^2][0].ShouldEqual("inspect");
}
