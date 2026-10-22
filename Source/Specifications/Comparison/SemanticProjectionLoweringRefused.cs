// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Comparison;

/// <summary>
/// The exception that identifies a failure while lowering an admitted per-run projection.
/// </summary>
/// <param name="cause">The named unsupported mapping failure.</param>
public sealed class SemanticProjectionLoweringRefused(Exception cause) : Exception(cause.Message, cause)
{
    /// <summary>
    /// Gets the projection whose lowering was refused.
    /// </summary>
    public SemanticId Projection { get; init; }
}
