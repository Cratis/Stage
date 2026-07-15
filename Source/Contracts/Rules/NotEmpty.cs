// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Requires the property value to not be empty (null, empty string, or empty collection).
/// </summary>
/// <param name="ErrorMessage">An optional custom error message.</param>
public record NotEmpty(string? ErrorMessage = null) : RuleDefinition(ErrorMessage);
