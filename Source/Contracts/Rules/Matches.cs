// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Requires the string property to match a regular expression pattern.
/// </summary>
/// <param name="Pattern">The regular expression pattern.</param>
/// <param name="ErrorMessage">An optional custom error message.</param>
public record Matches(string Pattern, string? ErrorMessage = null) : RuleDefinition(ErrorMessage);
