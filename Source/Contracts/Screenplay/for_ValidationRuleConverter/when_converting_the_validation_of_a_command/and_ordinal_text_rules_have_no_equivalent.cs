// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

public class and_ordinal_text_rules_have_no_equivalent : Specification
{
    [Theory]
    [InlineData(ValidationRuleKind.Contains)]
    [InlineData(ValidationRuleKind.NotContains)]
    [InlineData(ValidationRuleKind.StartsWith)]
    [InlineData(ValidationRuleKind.EndsWith)]
    void should_apply_the_documented_drop_instead_of_a_regex_substitute(ValidationRuleKind kind)
    {
        var rule = new ValidationRuleSyntax("reference", kind, new LiteralExpressionSyntax("literal", SourceLocation.Start), null, SourceLocation.Start);
        ValidationRuleConverter.Convert([new DeclarativeValidateSyntax([rule], SourceLocation.Start)]).ShouldBeEmpty();
    }
}
