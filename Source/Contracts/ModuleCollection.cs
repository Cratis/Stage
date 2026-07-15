// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// Represents a collection of modules within an event model.
/// </summary>
/// <param name="Id">The unique identifier of the module collection.</param>
/// <param name="EventModelId">The identifier of the event model the collection belongs to.</param>
/// <param name="Modules">The modules within the collection.</param>
public record ModuleCollection(
    Guid Id,
    Guid EventModelId,
    IReadOnlyList<Module> Modules);
