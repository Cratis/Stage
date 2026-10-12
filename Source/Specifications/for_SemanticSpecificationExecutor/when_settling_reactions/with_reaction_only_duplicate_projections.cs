// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_reaction_only_duplicate_projections : given.a_projected_reaction
{
    void Establish() => CompileProjected(expected: "then Followed\n  item = \"one\"", shape: "duplicate");
    Task Because() => Execute();
    [Fact] void should_refuse_instead_of_failing_replay() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
}
