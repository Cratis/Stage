// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_incompatible_scoped_identity : given.a_projected_reaction
{
    void Establish() => CompileProjected(expected: "then Followed\n  item = \"one\"", shape: "scoped", reaction: "reaction Worker\n  when Started\n    item\n    amount\n    produces Followed\n      for amount\n      item = item");
    Task Because() => Execute();
    [Fact] void should_refuse_incompatible_produced_identity_types() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
}
