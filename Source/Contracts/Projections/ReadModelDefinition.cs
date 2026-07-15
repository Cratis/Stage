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
    ProjectionDefinition? Projection);
