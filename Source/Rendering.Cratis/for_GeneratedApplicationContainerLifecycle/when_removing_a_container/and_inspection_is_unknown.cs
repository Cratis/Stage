// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_inspection_is_unknown : a_container
{
    ContainerRemovalResult _result = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        _docker.On("inspect", Inspected(Own, _owner), Failure("Cannot connect to the Docker daemon"));
        await _container.Create();
    }

    async Task Because() => _result = await _container.Remove();

    [Fact] void should_report_unknown() => _result.Outcome.ShouldEqual(ContainerRemoval.Unknown);
    [Fact] void should_not_succeed() => _result.Succeeded.ShouldBeFalse();
    [Fact] void should_issue_no_removal() => _docker.Count("rm").ShouldEqual(0);
}
