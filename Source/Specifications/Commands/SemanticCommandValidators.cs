// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Validation;
using Cratis.Stage.Api;
using FluentValidation;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Prevents Arc's separately discovered validators from altering Screenplay validation semantics.
/// SemanticRuleEvaluation checks every admitted rule, concept and requirement before Arc dispatch.
/// </summary>
/// <param name="context">The admitted specification context.</param>
internal sealed class SemanticCommandValidators(SemanticRunContext context) : IDiscoverableValidators
{
    readonly IValidator _validator = new InlineValidator<DynamicCommand>();

    /// <inheritdoc/>
    public bool TryGet(Type modelType, [MaybeNullWhen(false)] out IValidator validator)
    {
        validator = _validator;
        return modelType == context.CommandType;
    }

    /// <inheritdoc/>
    public bool TryGet(Type modelType, IServiceProvider serviceProvider, [MaybeNullWhen(false)] out IValidator validator) => TryGet(modelType, out validator);
}
