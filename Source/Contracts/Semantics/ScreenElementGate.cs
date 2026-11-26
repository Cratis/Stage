// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// A screen element with its own availability gate.
/// </summary>
/// <param name="Screen">The name of the screen holding it.</param>
/// <param name="Kind">Whether it is an action or a data element.</param>
/// <param name="Name">The name of the command or query it is bound to.</param>
/// <param name="Key">The exact marker key that carries its gate.</param>
/// <param name="Requirements">The gate of the element alone; the gates above it apply as well.</param>
public sealed record ScreenElementGate(string Screen, ScreenElementKind Kind, string Name, string Key, ImmutableArray<SemanticAvailabilityRequirement> Requirements);
