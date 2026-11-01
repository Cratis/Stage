// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class EsmRangeSurface
{
    internal static ImmutableHashSet<string> Gated { get; } = SemanticSurfaceLedger.Entries
        .Where(entry => entry.Value.Kind == SemanticSurfaceDispositionKind.Rejected && entry.Value.Detail == "STAGE-ESM-016")
        .Select(entry => entry.Key).ToImmutableHashSet(StringComparer.Ordinal);

    internal static ImmutableDictionary<string, SemanticSurfaceDisposition> Known { get; } = SemanticSurfaceLedger.Entries
        .Where(entry => !Gated.Contains(entry.Key)).ToImmutableDictionary(StringComparer.Ordinal);

    internal static SemanticUnknownMemberScope Scope(IReadOnlySet<SemanticId> owners, IReadOnlySet<SemanticId> excluded) => new(owners, excluded, Gated);
}
