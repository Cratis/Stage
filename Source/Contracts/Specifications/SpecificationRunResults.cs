// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents the root document written by the specification runner CLI — the results for every
/// specification it ran in a single invocation. This same object structure is read back and persisted.
/// </summary>
/// <param name="EventModelId">The identifier of the event model the specifications belong to.</param>
/// <param name="Results">The result for each specification that was run.</param>
public record SpecificationRunResults(
    Guid EventModelId,
    IReadOnlyList<SpecificationRunResult> Results);
