// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_handler_specifications.with_a_contradicting_body.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_handler_specifications;

public class with_a_contradicting_body(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_the_generated_specification() => fixture.Results.Length.ShouldBeGreaterThan(0);
    [Fact] void should_fail_the_event_payload_assertion() => fixture.Results.Any(result => result.Name.Contains("should_have_appended_order_placed_with_name", StringComparison.Ordinal) && result.Outcome == "Failed").ShouldBeTrue();

    public class context : given.a_handler_application
    {
        protected override string Body => "return new OrderPlaced(context.Command.Name.ToLowerInvariant());";
        Task Because() => ExecuteSpecifications(allowFailedTests: true);
    }
}
#endif
