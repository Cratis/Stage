// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_starting_a_container;

public class and_the_start_fails : a_container
{
    ContainerOutcome _outcome = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
        _docker.On("start", Failure("cannot start"));
        _docker.On("rm", Success());
        await _container.Create();
    }

    async Task Because()
    {
        _outcome = await _container.Start();
        await _container.Remove();
    }

    [Fact] void should_report_the_failure() => _outcome.Succeeded.ShouldBeFalse();
    [Fact] void should_start_by_identifier() => Commands("start").Single().ShouldEqual($"start {Own}");
    [Fact] void should_remove_exactly_once_by_identifier() => Commands("rm").Single().ShouldEqual($"rm --force --volumes {Own}");
}
