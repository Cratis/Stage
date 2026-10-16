// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_valid_members : given.a_composite_input
{
    void Establish() => Prepare("Details", "{\"count\":7,\"label\":\"Xxx\"}");
    Task Because() => Execute();

    [Fact] void should_pass_in_both_engines() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_have_no_failures() => _failures.ShouldBeEmpty();
}
