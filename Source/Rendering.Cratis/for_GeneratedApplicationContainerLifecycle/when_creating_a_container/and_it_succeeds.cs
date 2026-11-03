// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_creating_a_container;

public class and_it_succeeds : a_container
{
    ContainerOutcome _outcome = null!;

    void Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
    }

    async Task Because() => _outcome = await _container.Create();

    [Fact] void should_succeed() => _outcome.Succeeded.ShouldBeTrue();
    [Fact] void should_label_the_container_with_six_owner_labels() => _docker.Calls.Single(call => call[0] == "create").Count(argument => argument == "--label").ShouldEqual(6);
    [Fact] void should_label_the_container_with_its_run_token() => Commands("create").Single().ShouldContain("--label io.cratis.stage.harness.run=0123abcd0123abcd");
    [Fact] void should_write_the_identifier_to_the_private_file() => Commands("create").Single().ShouldContain($"--cidfile {_cidFile}");
    [Fact] void should_confirm_the_identifier_by_inspecting_it() => Commands("inspect").Single().ShouldContain($" {Own}");
    [Fact] void should_run_no_start_or_remove() => _docker.Calls.Count.ShouldEqual(2);
}
