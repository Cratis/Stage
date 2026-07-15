// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Events;
using Cratis.Stage.Contracts.Projections;
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
public record Slice(
    Guid Id,
    string Name,
    SliceType SliceType,
    IReadOnlyList<EventDefinition> Events,
    CommandDefinition? Command,
    ReadModelDefinition? ReadModel,
    IReadOnlyList<Specification> Specifications);
