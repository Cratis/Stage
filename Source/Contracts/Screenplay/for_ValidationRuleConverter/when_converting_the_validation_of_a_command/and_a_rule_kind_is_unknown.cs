// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

public class and_a_rule_kind_is_unknown : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_instead_of_dropping_even_a_non_literal_operand(bool nonLiteral)
    {
        var rule = new ValidationRuleSyntax(
            "reference",
            (ValidationRuleKind)999,
            nonLiteral ? new PathExpressionSyntax("other", SourceLocation.Start) : new LiteralExpressionSyntax("value", SourceLocation.Start),
            null,
            SourceLocation.Start);
        var error = Catch.Exception(() => ValidationRuleConverter.Convert([new DeclarativeValidateSyntax([rule], SourceLocation.Start)]));
        error.ShouldBeOfExactType<UnsupportedValidationRule>();
        error.Message.ShouldContain("STAGE-ESM-005");
        error.Message.ShouldContain("999");
        error.Message.ShouldContain("reference");
    }
}
