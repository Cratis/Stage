// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>The semantic owner and evidence associated with a traversal location.</summary>
/// <param name="Owner">The dependent declaration owner.</param>
/// <param name="Kind">The inherited reference category.</param>
/// <param name="Member">The member carrying the reference.</param>
/// <param name="PropertyTypeOf">The owner of the property type subtree, if any.</param>
internal readonly record struct SemanticDependencyScope(SemanticId Owner, SemanticDependencyKind Kind, string Member, SemanticId? PropertyTypeOf = null);
