// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Constraints;

/// <summary>
/// Represents the event type and properties tracked within a unique constraint.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="Properties">The property names included in the uniqueness check.</param>
public record UniqueConstraintEvent(
    string EventType,
    IReadOnlyList<string> Properties);
