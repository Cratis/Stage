// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_an_expected_rejection_feeds_an_unadmittable_projection : given.a_universal_projection
{
    void Establish() => _plan = ExpectRejection(CreatePlan(true, mapping: "\"fixed\""));

    async Task Because() => await Run();

    [Fact] void should_refuse_the_projection() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_name_the_projection() => _result.Unsupported!.Construct.ShouldEqual(_plan.Projections.Keys.Single().ToString());
    [Fact] void should_not_report_a_rejection() => _result.Trace.ShouldBeNull();
}
