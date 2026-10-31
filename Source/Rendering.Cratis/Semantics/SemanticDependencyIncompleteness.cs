// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Identifies a structural gap without parsing its diagnostic text.</summary>
internal enum SemanticDependencyIncompletenessKind
{
    UnknownVariant,
    UnclassifiedMember,
    UnresolvedIdentity,
    UnresolvedPolicy,
    DuplicateOwner
}

/// <summary>Structural evidence for a traversal location whose dependencies are not fully known.</summary>
/// <param name="Kind">The structural reason traversal is incomplete.</param>
/// <param name="Owner">The lexical declaration owner, if known.</param>
/// <param name="Member">The reference-carrying member.</param>
/// <param name="PropertyTypeOf">The owner of the property type subtree, if any.</param>
/// <param name="Reason">The existing human-readable traversal diagnostic.</param>
internal sealed record SemanticDependencyIncompleteness(SemanticDependencyIncompletenessKind Kind, SemanticId? Owner, string Member, SemanticId? PropertyTypeOf, string Reason);
