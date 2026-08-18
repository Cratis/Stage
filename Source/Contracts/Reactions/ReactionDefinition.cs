// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Reactions;

/// <summary>
/// Represents a <c>reaction</c> declared within a slice - behavior that runs when something happens.
/// </summary>
/// <param name="Id">The unique identifier of the reaction.</param>
/// <param name="Name">The name of the reaction.</param>
/// <param name="Description">The description of what the reaction does, or an empty string when none is declared.</param>
/// <param name="Where">The condition narrowing which occurrences run the reaction, or <see langword="null"/>
/// when every occurrence does.</param>
/// <param name="Triggers">What sets the reaction off, and what it does when set off.</param>
/// <remarks>
/// <c>reaction</c> rather than <c>reactor</c>, matching the language: a reactor is Chronicle's event observer
/// and says the trigger is always a domain event, where a reaction is the behavior and does not need to know
/// whether the trigger came from the event store, a clock or an integration.
/// </remarks>
public record ReactionDefinition(
    Guid Id,
    string Name,
    string Description,
    ProducedEventCondition? Where,
    IReadOnlyList<ReactionTrigger> Triggers);
