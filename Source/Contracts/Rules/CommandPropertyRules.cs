// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Represents the set of validation rules for a single command property.
/// </summary>
/// <param name="PropertyName">The name of the property the rules apply to.</param>
/// <param name="Rules">The ordered list of validation rules for the property.</param>
public record CommandPropertyRules(
    string PropertyName,
    IReadOnlyList<RuleDefinition> Rules);
