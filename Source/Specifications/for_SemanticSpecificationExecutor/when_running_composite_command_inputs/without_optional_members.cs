// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class without_optional_members : given.a_composite_input
{
    void Establish() => Prepare("Details", "{}");
    Task Because() => Execute();

    [Fact] void should_pass_in_both_engines() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_not_validate_omitted_members() => _failures.ShouldBeEmpty();
}
