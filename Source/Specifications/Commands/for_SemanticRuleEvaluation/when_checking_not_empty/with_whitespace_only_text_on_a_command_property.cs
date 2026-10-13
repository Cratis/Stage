// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_whitespace_only_text_on_a_command_property : given.a_not_empty_rule
{
    void Establish() => Prepare("\"   \"");
    void Because() => _failure = SemanticRuleEvaluation.FirstFailure(_command, _specification, _concepts);

    [Fact] void should_reject_with_the_authored_message() => _failure.ShouldEqual("Name is required");
}
