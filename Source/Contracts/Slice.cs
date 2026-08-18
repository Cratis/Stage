// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Captures;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Events;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Contracts.Reactions;
using Cratis.Stage.Contracts.Screens;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Represents a slice — the unit of behavior within a feature (command, events, read model).
/// </summary>
/// <param name="Id">The unique identifier of the slice.</param>
/// <param name="Name">The name of the slice.</param>
/// <param name="SliceType">The type of the slice.</param>
/// <param name="Events">The events defined within the slice.</param>
/// <param name="Command">The command defined within the slice, or <see langword="null"/> when the slice has none.</param>
/// <param name="ReadModel">The read model defined within the slice, or <see langword="null"/> when the slice has none.</param>
/// <param name="Specifications">The given/when/then specifications modeled on the slice.</param>
/// <remarks>
/// Capability added after the record shipped is an <c>init</c> property rather than a trailing parameter of the
/// primary constructor, deliberately. A trailing parameter on a positional record is source compatible and
/// <em>binary</em> breaking: it replaces the constructor and <c>Deconstruct</c> in the compiled signature, so a
/// package built against the previous version fails at run time with a missing method and no compiler error
/// anywhere. Package validation fails the build on that, and is how this record should grow from here.
/// </remarks>
public record Slice(
    Guid Id,
    string Name,
    SliceType SliceType,
    IReadOnlyList<EventDefinition> Events,
    CommandDefinition? Command,
    ReadModelDefinition? ReadModel,
    IReadOnlyList<Specification> Specifications)
{
    /// <summary>
    /// Gets the reactions declared within the slice — the behavior that runs when something happens.
    /// </summary>
    public IReadOnlyList<ReactionDefinition> Reactions { get; init; } = [];

    /// <summary>
    /// Gets the screens declared within the slice — which read models the slice's user interface shows, and
    /// which commands it offers.
    /// </summary>
    public IReadOnlyList<ScreenDefinition> Screens { get; init; } = [];

    /// <summary>
    /// Gets the captures declared within the slice — the translations of external sources into events.
    /// </summary>
    public IReadOnlyList<CaptureDefinition> Captures { get; init; } = [];
}
