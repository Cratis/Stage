// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_an_absent_parent : given.a_composite_input
{
    void Establish()
    {
        Prepare("Details optional", "{}");
        ChangeInput(SemanticValue.Null);
    }
    Task Because() => Execute();

    [Fact] void should_pass_in_both_engines() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_not_validate_absent_children() => _failures.ShouldBeEmpty();
}
