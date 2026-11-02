// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_command_inputs;

public class with_an_invalid_nested_checksum : given.a_composite_input
{
    void Establish() => Prepare("CheckedDetails", "{\"code\":\"79927398714\",\"count\":2}", "A value must end in the exact 'luhn' check digits.");
    Task Because() => Execute();

    [Fact] void should_match_reference_rejection() => (_reference.Passed && _result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_preserve_reference_messages_severities_and_order() => VerifyReferenceFailures();
    [Fact] void should_report_the_default_checksum_message_in_supplied_member_order() => _failures.Select(failure => failure.Message).ShouldEqual(["A value must end in the exact 'luhn' check digits.", "Count minimum", "Count excluded"]);
}
