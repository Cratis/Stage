// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents a given/when/then specification modeled on a slice.
/// </summary>
/// <param name="Id">The unique identifier of the specification.</param>
/// <param name="Name">The name of the specification.</param>
/// <param name="Given">The events that establish the Given precondition.</param>
/// <param name="When">The command that is executed in the When step, or <see langword="null"/> when none is set.</param>
/// <param name="ThenEvents">The events expected in the Then step.</param>
/// <param name="ThenErrors">The errors expected in the Then step.</param>
public record Specification(
    Guid Id,
    string Name,
    IReadOnlyList<SpecificationGivenEvent> Given,
    SpecificationCommand? When,
    IReadOnlyList<SpecificationThenEvent> ThenEvents,
    IReadOnlyList<SpecificationError> ThenErrors);
