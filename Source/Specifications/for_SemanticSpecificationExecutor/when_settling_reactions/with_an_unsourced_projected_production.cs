// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_unsourced_projected_production : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    produces Completed", "then Completed", new() { Action = "when append Started\n  item = \"one\"\n  amount = 1" });
    Task Because() => Execute();
    [Fact] void should_explicitly_refuse_the_observed_null_source() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
}
