// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_surrounding_whitespace : given.a_not_empty_rule
{
    void Establish() => Prepare("\" a \"");
    void Because() => _failure = SemanticRuleEvaluation.FirstFailure(_command, _specification, _concepts);

    [Fact] void should_accept_text_containing_non_whitespace() => _failure.ShouldBeNull();
}
