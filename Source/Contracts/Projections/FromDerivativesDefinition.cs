// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents projecting from a set of event types that derive from a common base type.
/// </summary>
/// <param name="EventTypes">The event type names sharing the base type.</param>
/// <param name="From">The from definition applied to those event types.</param>
public record FromDerivativesDefinition(
    IReadOnlyList<string> EventTypes,
    FromDefinition From);
