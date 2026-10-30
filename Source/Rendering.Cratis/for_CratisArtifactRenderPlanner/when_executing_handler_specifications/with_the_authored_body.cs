// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_handler_specifications.with_the_authored_body.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_handler_specifications;

public class with_the_authored_body(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_the_generated_specification() => fixture.Results.Length.ShouldBeGreaterThan(0);
    [Fact] void should_pass_every_then_assertion() => fixture.Results.All(result => result.Outcome == "Passed").ShouldBeTrue();

    public class context : given.a_handler_application
    {
        Task Because() => ExecuteSpecifications();
    }
}
#endif
