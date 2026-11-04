// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_removing_a_container;

public class and_the_run_label_differs : a_container
{
    ContainerRemovalResult _result = null!;

    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        _docker.On("inspect", Inspected(Own, _owner), Inspected(Own, OwnedBy("ffffffff", Now.ToUnixTimeSeconds())));
        await _container.Create();
    }

    async Task Because() => _result = await _container.Remove();

    [Fact] void should_refuse() => _result.Outcome.ShouldEqual(ContainerRemoval.Refused);
    [Fact] void should_issue_no_removal() => _docker.Count("rm").ShouldEqual(0);
}
