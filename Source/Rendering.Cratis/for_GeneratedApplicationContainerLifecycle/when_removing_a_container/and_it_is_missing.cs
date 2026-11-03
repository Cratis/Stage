// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_it_is_missing : a_container
{
    ContainerRemovalResult _result = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        _docker.On("inspect", Inspected(Own, _owner), Missing());
        await _container.Create();
    }

    async Task Because() => _result = await _container.Remove();

    [Fact] void should_report_it_already_gone() => _result.Outcome.ShouldEqual(ContainerRemoval.AlreadyGone);
    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_issue_no_removal() => _docker.Count("rm").ShouldEqual(0);
}
