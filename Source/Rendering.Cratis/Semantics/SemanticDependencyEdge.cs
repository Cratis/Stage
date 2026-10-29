// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>A reference from a dependent owner to the owner it requires.</summary>
/// <param name="Dependent">The owner that requires the dependency.</param>
/// <param name="Dependency">The referenced owner, normalized from any member identity.</param>
/// <param name="Kind">The reference category.</param>
/// <param name="Member">The semantic member carrying the reference.</param>
internal sealed record SemanticDependencyEdge(SemanticId Dependent, SemanticId Dependency, SemanticDependencyKind Kind, string Member);
