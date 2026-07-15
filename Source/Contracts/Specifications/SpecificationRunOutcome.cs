// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents the outcome of running a specification or one of its steps.
/// </summary>
public enum SpecificationRunOutcome
{
    /// <summary>The specification (or step) matched the modeled expectation.</summary>
    Passed,

    /// <summary>The specification (or step) did not match the modeled expectation.</summary>
    Failed,

    /// <summary>The specification (or step) could not be verified for this slice type yet.</summary>
    Inconclusive,
}
