// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Validation;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Exposes only the reference-compatible command validation subset to Arc's validation pipeline.
/// </summary>
/// <param name="context">The admitted specification context.</param>
internal sealed class SemanticCommandValidators(SemanticRunContext context) : IDiscoverableValidators
{
    readonly IValidator _validator = new Validator(context);

    /// <inheritdoc/>
    public bool TryGet(Type modelType, [MaybeNullWhen(false)] out IValidator validator)
    {
        validator = _validator;
        return modelType == context.CommandType;
    }

    /// <inheritdoc/>
    public bool TryGet(Type modelType, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IValidator validator) => TryGet(modelType, out validator);

    sealed class Validator : AbstractValidator<DynamicCommand>
    {
        public Validator(SemanticRunContext context)
        {
            RuleFor(command => command).Custom((_, validation) =>
            {
                var values = context.Specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
                foreach (var rule in context.Command.Validations)
                {
                    var value = values[rule.Property];
                    var bound = (rule.Operand as SemanticNumberValue)?.Value;
                    var measure = value switch
                    {
                        SemanticTextValue text => text.Value.Length,
                        SemanticNumberValue number => number.Value,
                        _ => 0m
                    };
                    var satisfied = rule.Kind switch
                    {
                        SemanticValidationRuleKind.NotEmpty => !SemanticRunContext.Empty(value),
                        SemanticValidationRuleKind.Minimum => value is SemanticNullValue || measure >= bound,
                        SemanticValidationRuleKind.Maximum => value is SemanticNullValue || measure <= bound,
                        _ => false
                    };
                    if (!satisfied)
                    {
                        validation.AddFailure(new ValidationFailure(rule.Property.ToString(), rule.Message!));
                    }
                }
            });
        }
    }
}
