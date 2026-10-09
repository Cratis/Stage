// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a read model defined within a slice.
/// </summary>
/// <param name="Id">The unique identifier of the read model.</param>
/// <param name="Name">The name of the read model.</param>
/// <param name="Schema">The JSON schema describing the read model's shape.</param>
/// <param name="Projection">The projection that builds the read model from events, or <see langword="null"/> when none is defined.</param>
public record ReadModelDefinition(
    Guid Id,
    string Name,
    string Schema,
    ProjectionDefinition? Projection)
{
    /// <summary>
    /// Gets the name of the Screenplay projection selected to build this read model. Conversion selects the first
    /// projection declared in the slice; <see langword="null"/> means no projection was selected or its source is unknown.
    /// </summary>
    public string? SelectedProjectionName { get; init; }

    /// <summary>
    /// Gets the names of the slice's other Screenplay projections, in declaration order, which were not converted.
    /// Empty when no projections were ignored or their source is unknown.
    /// </summary>
    public IReadOnlyList<string> IgnoredProjectionNames { get; init; } = [];

    /// <summary>
    /// Gets the modeled queries over this read model, in declaration order. A query with a parameter is served
    /// narrowed by that parameter; empty when the slice declares no query for the read model or its source is unknown.
    /// </summary>
    public IReadOnlyList<ReadModelQueryDefinition> Queries { get; init; } = [];
}
