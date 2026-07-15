// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// Represents the root of an event model — the structure the engine runs.
/// </summary>
/// <param name="Id">The unique identifier of the event model.</param>
/// <param name="Name">The name of the event model.</param>
/// <param name="Collections">The module collections within the event model.</param>
public record EventModel(
    Guid Id,
    string Name,
    IReadOnlyList<ModuleCollection> Collections);
