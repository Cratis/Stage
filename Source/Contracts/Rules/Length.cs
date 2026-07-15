// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Requires the string property to have a length within the specified range.
/// </summary>
/// <param name="Min">The minimum required length.</param>
/// <param name="Max">The maximum allowed length.</param>
/// <param name="ErrorMessage">An optional custom error message.</param>
public record Length(int Min, int Max, string? ErrorMessage = null) : RuleDefinition(ErrorMessage);
