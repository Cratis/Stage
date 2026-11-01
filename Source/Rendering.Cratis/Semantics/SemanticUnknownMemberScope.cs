// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed class SemanticUnknownMemberScope(
    IReadOnlySet<SemanticId> owners,
    IReadOnlySet<SemanticId> excluded,
    IReadOnlySet<string> gated)
{
    internal IReadOnlySet<SemanticId> Owners { get; } = owners.ToImmutableHashSet();
    internal IReadOnlySet<SemanticId> Excluded { get; } = excluded.ToImmutableHashSet();
    internal IReadOnlySet<string> Gated { get; } = gated.ToImmutableHashSet(StringComparer.Ordinal);
}
