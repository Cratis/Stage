// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_creating_a_container;

public class and_the_identifier_file_is_malformed : a_container
{
    ContainerOutcome _outcome = null!;

    void Establish() => ScriptCreate("not-an-identifier", Success());

    async Task Because() => _outcome = await _container.Create();

    [Fact] void should_fail() => _outcome.Succeeded.ShouldBeFalse();
    [Fact] void should_issue_only_the_create_command() => _docker.Calls.Count.ShouldEqual(1);
}
