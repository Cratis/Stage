// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents the command set in the When step of a specification.
/// </summary>
/// <param name="Id">The unique identifier of the specification command.</param>
/// <param name="CommandId">The identifier of the slice command this item refers to.</param>
/// <param name="Name">The name of the command.</param>
/// <param name="Values">The JSON object of property values for the command.</param>
public record SpecificationCommand(
    Guid Id,
    Guid CommandId,
    string Name,
    string Values);
