// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Identifies which part of a given/when/then specification a run step corresponds to.
/// </summary>
public enum SpecificationRunStepKind
{
    /// <summary>Appending the Given events that establish the precondition.</summary>
    Given,

    /// <summary>Executing the When command (state change slices).</summary>
    When,

    /// <summary>Verifying the Then events were produced (state change slices).</summary>
    ThenEvents,

    /// <summary>Verifying the Then read-model state (state view slices).</summary>
    ThenReadModel,

    /// <summary>Verifying the Then errors (rejections / constraint violations) were produced.</summary>
    ThenErrors,

    /// <summary>Structural verification used when behavioral execution is not yet available.</summary>
    Structural,
}
