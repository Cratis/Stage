// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Contracts.Rules;
using FluentValidation;

namespace Cratis.Stage.Validation;

/// <summary>
/// A FluentValidation validator reconstructed at runtime from the <see cref="CommandPropertyRules"/> modeled on a
/// command. Rather than reproduce a rule engine, the modeled rules are translated one-to-one into the equivalent
/// FluentValidation rules and executed over the command's raw JSON payload, matching the semantics of the Cratis Arc
/// validation engine on both the C# and TypeScript sides (only non-null/non-empty rules reject an absent value;
/// length, comparison and format rules are no-ops on an absent value).
/// </summary>
public sealed class StageCommandValidator : AbstractValidator<IReadOnlyDictionary<string, JsonElement>>
{
    const string PhonePattern = @"^[\d\s()+-]+$";
    const string UrlPattern = "^https?://.+";

    /// <summary>
    /// Initializes a new instance of the <see cref="StageCommandValidator"/> class.
    /// </summary>
    /// <param name="rules">The modeled property rules to reconstruct as FluentValidation rules.</param>
    public StageCommandValidator(IReadOnlyList<CommandPropertyRules> rules)
    {
        foreach (var propertyRules in rules)
        {
            foreach (var rule in propertyRules.Rules)
            {
                Apply(propertyRules.PropertyName, rule);
            }
        }
    }

    static void Message<TProperty>(
        IRuleBuilderOptions<IReadOnlyDictionary<string, JsonElement>, TProperty> builder,
        RuleDefinition rule)
    {
        if (!string.IsNullOrEmpty(rule.ErrorMessage))
        {
            builder.WithMessage(rule.ErrorMessage);
        }
    }

    void Apply(string property, RuleDefinition rule)
    {
        switch (rule)
        {
            case NotEmpty:
                Message(RuleFor(values => values.GetString(property)).NotEmpty().OverridePropertyName(property), rule);
                break;
            case NotNull:
                Message(RuleFor(values => values.GetString(property)).NotNull().OverridePropertyName(property), rule);
                break;
            case MinLength minLength:
                Message(RuleFor(values => values.GetString(property)).MinimumLength(minLength.Min).OverridePropertyName(property), rule);
                break;
            case MaxLength maxLength:
                Message(RuleFor(values => values.GetString(property)).MaximumLength(maxLength.Max).OverridePropertyName(property), rule);
                break;
            case Length length:
                Message(RuleFor(values => values.GetString(property)).Length(length.Min, length.Max).OverridePropertyName(property), rule);
                break;
            case EmailAddress:
                Message(RuleFor(values => values.GetString(property)).EmailAddress().OverridePropertyName(property), rule);
                break;
            case Phone:
                Message(RuleFor(values => values.GetString(property)).Matches(PhonePattern).OverridePropertyName(property), rule);
                break;
            case Url:
                Message(RuleFor(values => values.GetString(property)).Matches(UrlPattern).OverridePropertyName(property), rule);
                break;
            case Matches matches:
                Message(RuleFor(values => values.GetString(property)).Matches(matches.Pattern).OverridePropertyName(property), rule);
                break;
            case GreaterThan greaterThan:
                Number(property, rule, builder => builder.GreaterThan(greaterThan.Threshold));
                break;
            case GreaterThanOrEqual greaterThanOrEqual:
                Number(property, rule, builder => builder.GreaterThanOrEqualTo(greaterThanOrEqual.Threshold));
                break;
            case LessThan lessThan:
                Number(property, rule, builder => builder.LessThan(lessThan.Threshold));
                break;
            case LessThanOrEqual lessThanOrEqual:
                Number(property, rule, builder => builder.LessThanOrEqualTo(lessThanOrEqual.Threshold));
                break;
        }
    }

    void Number(
        string property,
        RuleDefinition rule,
        Func<IRuleBuilderInitial<IReadOnlyDictionary<string, JsonElement>, double>, IRuleBuilderOptions<IReadOnlyDictionary<string, JsonElement>, double>> comparison)
    {
        var configured = comparison(RuleFor(values => values.GetNumber(property)!.Value))
            .OverridePropertyName(property)
            .When(values => values.GetNumber(property) is not null);

        Message(configured, rule);
    }
}
