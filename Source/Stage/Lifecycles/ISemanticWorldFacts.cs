// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Lifecycles;

/// <summary>
/// Reads the facts of the committed semantic world.
/// </summary>
public interface ISemanticWorldFacts
{
    /// <summary>
    /// Gets every committed fact in order.
    /// </summary>
    /// <returns>The facts.</returns>
    Task<ImmutableArray<SemanticFact>> Facts();
}
