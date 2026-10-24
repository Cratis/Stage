// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_aligning_projection_admission_with_runtime : given.a_projection_reachability_plan
{
    [Theory]
    [InlineData("unrelated", "recorded", true)]
    [InlineData("unrelated", "wrong", false)]
    [InlineData("every", "recorded", true)]
    public async Task should_evaluate_scalar_assertions_without_admitting_an_unrelated_projection(string observation, string expected, bool passes)
    {
        var plan = Compile(observation, expected);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        reference.Passed.ShouldEqual(passes);
        SemanticRunAdmission.Check(plan, specification).ShouldBeNull();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        result.Outcome.ShouldEqual(passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed);
        result.Failures.ShouldContainOnly(reference.Failures);
        Assert.StartsWith(plan.ReadModels.Values.Single(model => model.Name == "Current").Id.ToString() + ":", Assert.Single(result.Trace.ReadModels).Key);
        result.Unsupported.ShouldBeNull();
    }

    [Theory]
    [InlineData("from")]
    [InlineData("removal")]
    [InlineData("all")]
    [InlineData("flat")]
    [InlineData("seed")]
    [InlineData("assertion")]
    public async Task should_check_every_unadmittable_projection_the_runtime_would_request(string observation)
    {
        var plan = Compile(observation);
        var specification = plan.Specifications.Values.Single();
        var projection = plan.Projections.Values.Single(candidate => candidate.Name == "UnadmittableProjection");
        var requested = SemanticRunProjections.RequestedReadModels(plan, specification, specification, specification.ThenEvents);
        requested.ShouldContain(projection.ReadModel);
        var refusal = SemanticRunAdmission.Check(plan, specification);
        Assert.NotNull(refusal);
        refusal.Capability.ShouldEqual(StageExecutionCapability.Projection);
        refusal.Construct.ShouldEqual(projection.Id.ToString());

        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported.Capability.ShouldEqual(StageExecutionCapability.Projection);
        result.Unsupported.Construct.ShouldEqual(projection.Id.ToString());
        Assert.NotEmpty(result.Unsupported.Details);
        result.Trace.ShouldBeNull();
    }
}
