// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents an event a command produces — the modeled <c>produces</c> declaration.
/// </summary>
/// <param name="Event">The name of the event type to append.</param>
/// <param name="When">The condition guarding the production, or <see langword="null"/> when the event is always produced.</param>
/// <param name="Properties">How each property of the event payload gets its value.</param>
/// <param name="Tags">The tags to append the event with.</param>
public record ProducedEvent(
    string Event,
    ProducedEventCondition? When,
    IReadOnlyList<ProducedEventProperty> Properties,
    IReadOnlyList<string> Tags);
