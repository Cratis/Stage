// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Requires the string property to have at most the specified length.
/// </summary>
/// <param name="Max">The maximum allowed length.</param>
/// <param name="ErrorMessage">An optional custom error message.</param>
public record MaxLength(int Max, string? ErrorMessage = null) : RuleDefinition(ErrorMessage);
