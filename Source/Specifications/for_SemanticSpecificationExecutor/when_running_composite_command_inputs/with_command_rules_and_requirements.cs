// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_command_rules_and_requirements : given.a_composite_input
{
    void Establish()
    {
        Prepare("Details", "{\"count\":2,\"label\":\"bad\"}", "Command rule");
        var id = _command.Properties.Single(property => property.Name == "id").Id;
        var command = _command with
        {
            Validations = [new(id, SemanticValidationRuleKind.Equal, SemanticValue.Text("other"), "Command rule")],
            Requirements = [new(new SemanticComparison(new(id, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text("other"))), "Requirement")]
        };
        ChangeBehavior(_specification, command);
    }
    Task Because() => Execute();

    [Fact] void should_match_reference_rejection() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_validate_command_rules_then_concepts_then_requirements() => _failures.Select(failure => failure.Message).ShouldEqual(["Command rule", "Count minimum", "Count excluded", "Label pattern", "Requirement"]);
    [Fact] void should_preserve_reference_messages_and_severities() => _failures.ShouldEqual(((SemanticRejected)_reference.Execution).ValidationFailures);
}
