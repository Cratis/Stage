// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// Represents a module — a top-level domain area within an event model.
/// </summary>
/// <param name="Id">The unique identifier of the module.</param>
/// <param name="EventModelId">The identifier of the event model the module belongs to.</param>
/// <param name="CollectionId">The identifier of the module collection the module belongs to.</param>
/// <param name="Name">The name of the module.</param>
/// <param name="Features">The features within the module.</param>
public record Module(
    Guid Id,
    Guid EventModelId,
    Guid CollectionId,
    string Name,
    IReadOnlyList<Feature> Features);
