// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

/// <summary>
/// Screenplay lets a rule be stated against a value only known while the application runs — <c>dueDate &gt; today</c>,
/// or a threshold naming another property. A Stage rule holds a fixed operand and there is none to hold for these, so
/// they are dropped.
/// </summary>
/// <remarks>
/// The conversion used to substitute <c>0</c> for the missing number and an empty string for the missing pattern, which
/// is the failure worth pinning: <c>dueDate &gt; today</c> became "greater than zero" and <c>matches somePattern</c>
/// became "matches the empty pattern" — rules that assert something the document never said, carried with the same
/// confidence as the ones it did. A rule that is not carried can be reported; a rule carried with an invented operand
/// reads as faithful and is not.
/// </remarks>
public class and_an_operand_is_a_runtime_value : given.a_validation_block
{
    IReadOnlyList<CommandPropertyRules> _converted = null!;

    void Because() => _converted = ValidationRuleConverter.Convert([Block(_ => new PathExpressionSyntax("today", SourceLocation.Start))]);

    [Fact] void should_carry_no_rule_at_all() => _converted.ShouldBeEmpty();

    [Fact] void should_not_substitute_a_threshold() =>
        _converted.SelectMany(rules => rules.Rules).OfType<GreaterThan>().ShouldBeEmpty();

    [Fact] void should_not_substitute_a_pattern() =>
        _converted.SelectMany(rules => rules.Rules).OfType<Matches>().ShouldBeEmpty();
}
