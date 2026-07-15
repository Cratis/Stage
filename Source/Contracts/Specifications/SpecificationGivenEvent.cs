// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents an event in the Given step of a specification.
/// </summary>
/// <param name="Id">The unique identifier of the specification item.</param>
/// <param name="Name">The name of the event.</param>
/// <param name="EventId">The identifier of the slice event this item refers to.</param>
/// <param name="Values">The JSON object of property values for the event.</param>
public record SpecificationGivenEvent(
    Guid Id,
    string Name,
    Guid EventId,
    string Values);
