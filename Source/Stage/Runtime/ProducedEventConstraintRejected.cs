// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Stage.Runtime;

/// <summary>
/// The exception that is thrown when Chronicle rejects a produced event because it violates a constraint.
/// </summary>
/// <param name="validationResult">The constraint's name and message as a command validation result.</param>
public sealed class ProducedEventConstraintRejected(ValidationResult validationResult)
    : Exception(validationResult.Message), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult => validationResult;
}
