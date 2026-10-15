// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.given;

public class a_modeled_rule : Specification
{
    protected static IReadOnlyList<CommandPropertyRules> Rules(string property, ValidationRuleKind kind, object? operand = null) =>
        ValidationRuleConverter.Convert(
        [
            new DeclarativeValidateSyntax(
            [
                new ValidationRuleSyntax(property, kind, operand is null ? null : new LiteralExpressionSyntax(operand, SourceLocation.Start), "Modeled rejection", SourceLocation.Start)
            ],
            SourceLocation.Start)
        ]);
}
