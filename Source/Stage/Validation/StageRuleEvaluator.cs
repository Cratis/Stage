// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rules;

namespace Cratis.Stage.Validation;

/// <summary>
/// Evaluates the validation rules modeled on a command against a set of command values by reconstructing and
/// running a <see cref="StageCommandValidator"/> — the FluentValidation rule engine, rather than a hand-rolled one.
/// </summary>
public static class StageRuleEvaluator
{
    /// <summary>
    /// Evaluates the rules against the supplied JSON values and returns the resulting violations.
    /// </summary>
    /// <param name="rules">The modeled property rules.</param>
    /// <param name="valuesJson">The JSON object of command values.</param>
    /// <returns>The <see cref="StageRuleViolation"/> list; empty when every rule holds.</returns>
    public static IReadOnlyList<StageRuleViolation> Evaluate(IReadOnlyList<CommandPropertyRules> rules, string? valuesJson)
    {
        if (rules.Count == 0)
        {
            return [];
        }

        var validator = new StageCommandValidator(rules);
        var result = validator.Validate(JsonValues.Parse(valuesJson));

        return [.. result.Errors.Select(error => new StageRuleViolation(error.PropertyName, error.ErrorMessage))];
    }
}

/// <summary>
/// Represents a single rule violation produced while evaluating a command's rules.
/// </summary>
/// <param name="PropertyName">The name of the property whose rule was violated.</param>
/// <param name="Message">The violation message.</param>
public record StageRuleViolation(string PropertyName, string Message);
