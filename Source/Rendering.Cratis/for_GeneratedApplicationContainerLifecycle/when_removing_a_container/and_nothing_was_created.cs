// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_nothing_was_created : a_container
{
    ContainerRemovalResult _result = null!;

    async Task Because() => _result = await _container.Remove();

    [Fact] void should_do_nothing() => _result.Outcome.ShouldEqual(ContainerRemoval.NotCreated);
    [Fact] void should_issue_no_command() => _docker.Calls.ShouldBeEmpty();
}
