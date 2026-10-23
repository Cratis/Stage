// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_mismatched_flat_key : given.a_projected_reaction
{
    void Establish() => CompileProjected("\"two\"", "then no readmodel Marker for \"two\"");
    Task Because() => Execute();
    [Fact] void should_run_the_projection_keyed_by_the_event_property() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_expose_the_reference_failure() => _reference.Passed.ShouldBeFalse();
}
