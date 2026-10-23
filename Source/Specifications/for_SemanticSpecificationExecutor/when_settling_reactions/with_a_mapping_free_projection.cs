// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_mapping_free_projection : given.a_projected_reaction
{
    void Establish() => CompileProjected("\"two\"", "then no readmodel Marker for \"two\"", shape: "mapping-free");
    Task Because() => Execute();
    [Fact] void should_refuse_instead_of_falsely_passing_absence() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_expose_the_reference_result() => _reference.Passed.ShouldBeTrue();
}
