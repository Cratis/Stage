// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Stage.Api;

/// <summary>
/// The exception that is thrown when a command's input does not satisfy what the model declares for it - a required
/// property that is missing or a validation rule that does not hold. Nothing was appended, and the caller is told which
/// properties to correct instead of being answered with a server error.
/// </summary>
/// <param name="validationResult">The rejection as a command validation result naming the offending properties.</param>
public sealed class CommandInputRejected(ValidationResult validationResult)
    : Exception(validationResult.Message), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult => validationResult;
}
