// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_a_collection : given.a_composite_input
{
    void Establish() => Prepare("Details[] optional", "[{\"count\":2,\"label\":\"bad\"},{\"count\":2,\"label\":\"bad\"}]", "Count minimum");
    Task Because() => Execute();

    [Fact] void should_match_reference_rejection() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_visit_each_element_in_order() => _failures.Select(failure => failure.Message).ShouldEqual(["Count minimum", "Count excluded", "Label pattern", "Count minimum", "Count excluded", "Label pattern"]);
    [Fact] void should_preserve_reference_failure_order() => _failures.ShouldEqual([.. ((SemanticRejected)_reference.Execution).ValidationFailures]);
}
