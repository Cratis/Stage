// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_the_process_exits;

public class and_no_identifier_was_captured : a_container
{
    void Because() => _container.RemoveWhenExiting();

    [Fact] void should_issue_no_command() => _docker.Calls.ShouldBeEmpty();
}
