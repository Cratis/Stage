// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Screenplay.for_ValidationRuleConverter.given;

public class a_validation_block : Specification
{
    // Every rule kind that takes an operand, so a spec can offer the same set twice — once stating a literal and
    // once stating a value the application only resolves while it runs — and compare what survives each time.
    protected static readonly ValidationRuleKind[] KindsTakingAnOperand =
    [
        ValidationRuleKind.Min,
        ValidationRuleKind.Max,
        ValidationRuleKind.Length,
        ValidationRuleKind.Matches,
        ValidationRuleKind.GreaterThan,
        ValidationRuleKind.GreaterThanOrEqual,
        ValidationRuleKind.LessThan,
        ValidationRuleKind.LessThanOrEqual,
        ValidationRuleKind.AllGreaterThan,
        ValidationRuleKind.AllGreaterThanOrEqual
    ];

    // The property carries the kind's name so a converted rule can be traced back to the kind that produced it.
    protected static ValidateSyntax Block(Func<ValidationRuleKind, ExpressionSyntax> operand) =>
        new DeclarativeValidateSyntax(
            [.. KindsTakingAnOperand.Select(kind => new ValidationRuleSyntax(kind.ToString(), kind, operand(kind), null, SourceLocation.Start))],
            SourceLocation.Start);

    // 'matches' takes a pattern rather than a number, so it gets one — the point of the spec is the operand being
    // fixed, not what shape of fixed value each kind happens to want.
    protected static ExpressionSyntax Literal(ValidationRuleKind kind) =>
        new LiteralExpressionSyntax(kind == ValidationRuleKind.Matches ? "^INV-[0-9]{6}$" : 5d, SourceLocation.Start);
}
