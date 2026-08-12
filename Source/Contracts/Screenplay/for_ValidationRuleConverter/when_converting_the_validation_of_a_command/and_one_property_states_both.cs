// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

/// <summary>
/// The shape a real document takes: one property validated by several rules, only some of which state a fixed value.
/// Dropping the one that cannot be carried must not take the ones that can with it, and must not leave an empty group
/// behind that reads as a property with no invariants rather than a property with fewer.
/// </summary>
public class and_one_property_states_both : Specification
{
    IReadOnlyList<CommandPropertyRules> _converted = null!;

    void Because() => _converted = ValidationRuleConverter.Convert(
    [
        new DeclarativeValidateSyntax(
        [
            Rule("amount", ValidationRuleKind.GreaterThanOrEqual, new LiteralExpressionSyntax(0d, SourceLocation.Start)),
            Rule("amount", ValidationRuleKind.LessThan, new PathExpressionSyntax("creditLimit", SourceLocation.Start)),
            Rule("dueDate", ValidationRuleKind.GreaterThan, new PathExpressionSyntax("today", SourceLocation.Start))
        ],
        SourceLocation.Start)
    ]);

    [Fact] void should_only_carry_the_property_that_has_a_rule_left() =>
        _converted.Select(rules => rules.PropertyName).ShouldContainOnly(["amount"]);

    [Fact] void should_carry_the_rule_stating_a_fixed_value() =>
        _converted.Single().Rules.OfType<GreaterThanOrEqual>().Single().Threshold.ShouldEqual(0d);

    [Fact] void should_not_carry_the_rule_stating_another_property() =>
        _converted.Single().Rules.OfType<LessThan>().ShouldBeEmpty();

    static ValidationRuleSyntax Rule(string property, ValidationRuleKind kind, ExpressionSyntax value) =>
        new(property, kind, value, null, SourceLocation.Start);
}
