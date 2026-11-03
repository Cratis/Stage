// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_invalid_members : given.a_composite_input
{
    void Establish() => Prepare("Details", "{\"count\":2,\"label\":\"bad\"}", "Count minimum");
    Task Because() => Execute();

    [Fact] void should_match_reference_rejection() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_preserve_messages_severities_and_order() => VerifyReferenceFailures();
    [Fact] void should_visit_supplied_members_in_order() => _failures.Select(failure => failure.Message).ShouldEqual(["Count minimum", "Count excluded", "Label pattern"]);
}
