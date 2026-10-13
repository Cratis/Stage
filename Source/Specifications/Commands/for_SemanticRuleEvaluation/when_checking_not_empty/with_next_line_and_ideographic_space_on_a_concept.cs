// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_next_line_and_ideographic_space_on_a_concept : given.a_not_empty_rule
{
    void Establish() => Prepare("\"\u0085\u00a0\u3000\"", concept: true);
    void Because() => _failure = SemanticRuleEvaluation.FirstFailure(_command, _specification, _concepts);

    [Fact] void should_reject_unicode_whitespace_with_the_authored_message() => _failure.ShouldEqual("Name is required");
}
