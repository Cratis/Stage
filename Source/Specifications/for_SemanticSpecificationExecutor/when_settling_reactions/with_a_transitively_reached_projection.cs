// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_transitively_reached_projection : given.a_projected_reaction
{
    void Establish() => CompileProjected(shape: "literal", reaction: "reaction Invoke\n  when Started\n    item\n    invokes Complete\n      item = item\nreaction Follow\n  when Completed\n    produces Followed\n      item = \"one\"");
    Task Because() => Execute();
    [Fact] void should_refuse_the_projection_reached_through_an_invocation_and_cascade() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_refuse_at_initial_admission() => SemanticSpecificationAdmission.Check(_plan, _plan.Specifications.Values.Single())!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_preserve_the_reference_supported_outcome() => _reference.Passed.ShouldBeTrue();
}
