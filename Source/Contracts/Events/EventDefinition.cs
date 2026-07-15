// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Constraints;

namespace Cratis.Stage.Contracts.Events;

/// <summary>
/// Represents an event defined within a slice.
/// </summary>
/// <param name="Id">The unique identifier of the event.</param>
/// <param name="Name">The name of the event.</param>
/// <param name="SourceEventId">The identifier of the source event this item references when owned by another slice; empty for an owned event.</param>
/// <param name="Schema">The JSON schema describing the event's payload.</param>
/// <param name="UniqueEventTypeConstraint">The unique-event-type constraint applied to the event, or <see langword="null"/> when none.</param>
/// <param name="UniqueConstraint">The unique constraint applied to the event, or <see langword="null"/> when none.</param>
public record EventDefinition(
    Guid Id,
    string Name,
    string SourceEventId,
    string Schema,
    UniqueEventTypeConstraint? UniqueEventTypeConstraint,
    UniqueConstraint? UniqueConstraint);
