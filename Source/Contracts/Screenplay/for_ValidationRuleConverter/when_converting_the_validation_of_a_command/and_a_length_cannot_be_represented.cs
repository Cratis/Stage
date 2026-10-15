// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

public class and_a_length_cannot_be_represented : Specification
{
    IReadOnlyList<CommandPropertyRules> _converted = null!;

    void Because() => _converted = ValidationRuleConverter.Convert(
    [
        new DeclarativeValidateSyntax(
        [
            .. new[] { -1d, 1.5d, 2147483648d }.Select(length => new ValidationRuleSyntax("name", ValidationRuleKind.Length, new LiteralExpressionSyntax(length, SourceLocation.Start), null, SourceLocation.Start))
        ],
        SourceLocation.Start)
    ]);

    [Fact] void should_drop_the_rule_instead_of_truncating_or_overflowing_its_length() => _converted.ShouldBeEmpty();
}
