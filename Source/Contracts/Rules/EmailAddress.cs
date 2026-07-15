// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Requires the string property to be a valid email address.
/// </summary>
/// <param name="ErrorMessage">An optional custom error message.</param>
public record EmailAddress(string? ErrorMessage = null) : RuleDefinition(ErrorMessage);
