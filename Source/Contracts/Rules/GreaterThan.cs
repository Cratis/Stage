// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Requires the numeric property to be strictly greater than the specified threshold.
/// </summary>
/// <param name="Threshold">The exclusive lower bound.</param>
/// <param name="ErrorMessage">An optional custom error message.</param>
public record GreaterThan(double Threshold, string? ErrorMessage = null) : RuleDefinition(ErrorMessage);
