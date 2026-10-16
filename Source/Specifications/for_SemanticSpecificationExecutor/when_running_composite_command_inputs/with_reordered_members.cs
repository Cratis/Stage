// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_reordered_members : given.a_composite_input
{
    void Establish()
    {
        Prepare("Details", "{\"count\":2,\"label\":\"bad\"}", "Label pattern");
        var input = (SemanticCompositeValue)_specification.When!.Values[^1].Value;
        ChangeInput(SemanticValue.Composite([.. input.Properties.Reverse()]));
    }
    Task Because() => Execute();

    [Fact] void should_match_reference_rejection() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_preserve_supplied_order_not_declaration_order() => _failures.Select(failure => failure.Message).ShouldEqual(["Label pattern", "Count minimum", "Count excluded"]);
    [Fact] void should_match_reference_messages_and_severities() => _failures.ShouldEqual(((SemanticRejected)_reference.Execution).ValidationFailures);
}
