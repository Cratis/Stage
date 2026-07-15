// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Constraints;

/// <summary>
/// Represents a uniqueness constraint spanning one or more events and their properties.
/// </summary>
/// <param name="Name">The constraint name.</param>
/// <param name="Message">The violation message.</param>
/// <param name="IgnoreCasing">A value indicating whether string comparison ignores casing.</param>
/// <param name="EventsWithProperties">The events and their properties that form the uniqueness key.</param>
/// <param name="RemovedWith">The event type that removes the constraint, or <see langword="null"/> when none.</param>
public record UniqueConstraint(
    string Name,
    string Message,
    bool IgnoreCasing,
    IReadOnlyList<UniqueConstraintEvent> EventsWithProperties,
    string? RemovedWith);
