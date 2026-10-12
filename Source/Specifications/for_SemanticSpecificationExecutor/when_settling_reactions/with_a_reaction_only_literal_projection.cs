// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_reaction_only_literal_projection : given.a_projected_reaction
{
    void Establish() => CompileProjected(expected: "then Followed\n  item = \"one\"", shape: "literal");
    Task Because() => Execute();
    [Fact] void should_refuse_the_shape_before_replay() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_preserve_the_reference_supported_outcome() => _reference.Passed.ShouldBeTrue();
}
