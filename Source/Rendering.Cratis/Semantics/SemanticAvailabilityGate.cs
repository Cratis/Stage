// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// A gate on a namespace (a module, feature or slice) or on one generated type (a command or a read model).
/// </summary>
/// <param name="Key">The namespace or the full type name.</param>
/// <param name="IsType">Whether the gate is on a type.</param>
/// <param name="Requirements">The gate lines, every one of which must hold.</param>
/// <param name="Description">What the gate is on, for diagnostics.</param>
internal sealed record SemanticAvailabilityGate(string Key, bool IsType, ImmutableArray<SemanticAvailabilityRequirement> Requirements, string Description);
