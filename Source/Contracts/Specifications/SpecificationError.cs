// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents an expected error in the Then step of a specification.
/// </summary>
/// <param name="Id">The unique identifier of the specification item.</param>
/// <param name="Name">The name of the expected error.</param>
public record SpecificationError(
    Guid Id,
    string Name);
