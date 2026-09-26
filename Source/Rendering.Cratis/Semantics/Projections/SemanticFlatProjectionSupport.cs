// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Shares the complete flat mapping predicate between rendering and per-run admission.
/// </summary>
internal static class SemanticFlatProjectionSupport
{
    internal static bool MappingsMatch(
        ImmutableArray<SemanticPropertyMapping> mappings,
        ImmutableArray<SemanticProperty> targets,
        ImmutableArray<SemanticProperty> sources,
        SemanticExpressionRootKind root) =>
        mappings.Length == targets.Length && targets.All(target => mappings.Any(mapping => mapping.TargetProperty == target.Id &&
            mapping.Source is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
            resolved.Root == root && sources.Any(source => source.Id == resolved.Target)));
}
