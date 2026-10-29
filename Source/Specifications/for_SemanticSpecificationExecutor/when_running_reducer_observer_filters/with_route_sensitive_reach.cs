// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_reducer_observer_filters;

public class with_route_sensitive_reach : given.a_filtered_reducer
{
    [Theory]
    [InlineData("seed", "none", true, false)]
    [InlineData("given", "none", true, false)]
    [InlineData("given", "source", true, false)]
    [InlineData("given", "stream", true, false)]
    [InlineData("given", "match", true, true)]
    [InlineData("command", "none", true, false)]
    [InlineData("command", "source", true, false)]
    [InlineData("command", "stream", true, false)]
    [InlineData("command", "match", true, true)]
    [InlineData("append", "none", true, false)]
    [InlineData("append", "source", true, false)]
    [InlineData("append", "stream", true, false)]
    [InlineData("append", "match", true, true)]
    [InlineData("command", "none", false, false)]
    [InlineData("command", "source", false, false)]
    [InlineData("command", "stream", false, false)]
    [InlineData("command", "match", false, true)]
    [InlineData("given", "match", false, true)]
    [InlineData("query", "none", true, true)]
    public async Task should_match_reference_without_executing_a_body(string path, string route, bool seeded, bool refused)
    {
        var plan = Plan(path, route, seeded);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var retained = new List<int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => retained.Add(count) };
        var report = await executor.Run(plan, new([specification.Id]), new());
        var result = report.Results.Single();
        var unsupported = refused || seeded;
        Assert.True(reference.Execution.Kind == (unsupported ? SemanticExecutionOutcomeKind.Unsupported : SemanticExecutionOutcomeKind.Accepted), reference.Execution.ToString());
        result.Outcome.ShouldEqual(unsupported ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed);
        if (unsupported)
        {
            ((SemanticUnsupported)reference.Execution).Capability.ShouldEqual(path == "query" ? SemanticExecutionCapability.Query : SemanticExecutionCapability.Projection);
            result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
            result.Unsupported.Construct.ShouldEqual(plan.ReadModels.Keys.Single().ToString());
            result.Unsupported.Details.ShouldContain("STAGE-IMPL-003:");
            var retainedFacts = RetainedFacts(seeded, refused, path);
            retained.TrueForAll(count => count == retainedFacts).ShouldBeTrue();
        }
        else
        {
            reference.Passed.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task should_check_constraints_before_reducer_reach_for_the_whole_command_batch()
    {
        var plan = Plan("command", "match", true, constrained: true);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        ((SemanticRejected)reference.Execution).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
        reference.Passed.ShouldBeTrue();
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        result.Trace!.RejectionCode.ShouldEqual("Once");
        result.Trace.Facts.ShouldBeEmpty();
    }

    static int RetainedFacts(bool seeded, bool refused, string path) => seeded && !refused && path != "seed" ? 1 : 0;
}
