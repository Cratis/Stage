// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

public class and_a_rule_has_no_stage_equivalent : Specification
{
    IReadOnlyList<CommandPropertyRules> _converted = null!;

    void Because() => _converted = ValidationRuleConverter.Convert(
    [
        new DeclarativeValidateSyntax(
        [
            .. new[] { ValidationRuleKind.AllGreaterThan, ValidationRuleKind.AllGreaterThanOrEqual, ValidationRuleKind.Equal, ValidationRuleKind.NotEqual, ValidationRuleKind.Checksum, ValidationRuleKind.Rule }
                .Select(kind => new ValidationRuleSyntax("lines.quantity", kind, new LiteralExpressionSyntax(1d, SourceLocation.Start), null, SourceLocation.Start))
        ],
        SourceLocation.Start)
    ]);

    [Fact] void should_drop_the_rules_instead_of_substituting_scalar_comparisons() => _converted.ShouldBeEmpty();
}
