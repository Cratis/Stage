// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.when_converting_the_validation_of_a_command;

/// <summary>
/// The baseline the dropping specs are read against: supported scalar rules stating fixed values are carried.
/// Without it, a converter that dropped everything would satisfy the dropping specs unnoticed.
/// </summary>
public class and_every_operand_is_a_literal : given.a_validation_block
{
    IReadOnlyList<CommandPropertyRules> _converted = null!;

    void Because() => _converted = ValidationRuleConverter.Convert([Block(Literal)]);

    [Fact] void should_carry_every_supported_scalar_kind() =>
        _converted.Select(rules => rules.PropertyName).ShouldContainOnly(KindsTakingAnOperand.Where(kind => kind is not (ValidationRuleKind.AllGreaterThan or ValidationRuleKind.AllGreaterThanOrEqual)).Select(kind => kind.ToString()));

    [Fact] void should_carry_the_stated_threshold() =>
        _converted.Single(rules => rules.PropertyName == nameof(ValidationRuleKind.GreaterThan))
            .Rules.OfType<GreaterThan>().Single().Threshold.ShouldEqual(5d);

    [Fact] void should_carry_the_stated_pattern() =>
        _converted.Single(rules => rules.PropertyName == nameof(ValidationRuleKind.Matches))
            .Rules.OfType<Matches>().Single().Pattern.ShouldEqual("^INV-[0-9]{6}$");
}
