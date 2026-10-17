// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_a_lone_byte_order_mark : given.a_not_empty_rule
{
    void Establish() => Prepare("\"\ufeff\"", concept: true);
    void Because() => _failure = SemanticRuleEvaluation.FirstFailure(_command, _specification, _application);

    [Fact] void should_accept_the_non_whitespace_format_character() => _failure.ShouldBeNull();
}
