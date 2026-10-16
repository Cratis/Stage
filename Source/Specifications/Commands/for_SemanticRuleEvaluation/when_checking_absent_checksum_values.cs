// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_absent_checksum_values
{
    [Theory]
    [InlineData("luhn")]
    [InlineData("mod11")]
    [InlineData("mod97")]
    public void should_leave_presence_to_not_empty(string algorithm) => SemanticRuleValues.Satisfies(
        new(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text(algorithm), null),
        SemanticValue.Null,
        () => new UnsupportedSemanticMapping()).ShouldBeTrue();
}
