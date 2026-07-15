// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Constraints;

/// <summary>
/// Represents a unique-event-type constraint applied to an event.
/// </summary>
/// <param name="Name">The constraint name.</param>
/// <param name="Message">The violation message.</param>
/// <param name="EventType">The event type that must be unique.</param>
/// <param name="RemovedWith">The event type that removes the constraint, or <see langword="null"/> when none.</param>
public record UniqueEventTypeConstraint(
    string Name,
    string Message,
    string EventType,
    string? RemovedWith);
