// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents a read model state in a specification — used for both the Given precondition and the Then expectation.
/// </summary>
/// <param name="Id">The unique identifier of the specification item.</param>
/// <param name="Name">The name of the read model.</param>
/// <param name="ReadModelId">The identifier of the slice read model this item refers to.</param>
/// <param name="Values">The JSON object of property values for the read model.</param>
public record SpecificationReadModel(
    Guid Id,
    string Name,
    Guid ReadModelId,
    string Values);
