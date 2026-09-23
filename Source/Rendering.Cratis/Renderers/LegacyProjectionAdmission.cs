// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Rejects legacy projection blocks that model-bound attributes cannot preserve before emitting any source.
/// </summary>
internal static class LegacyProjectionAdmission
{
    public static void EnsureSupported(ProjectionSyntax projection, EventPropertyIndex events)
    {
        if (projection.Sequence is not null || projection.Blocks.OfType<EverySyntax>().Skip(1).Any())
        {
            throw new UnsupportedLegacyProjection(projection.Name, "sequence or repeated every");
        }

        foreach (var block in projection.Blocks)
        {
            if (block is AllSyntax or ClearWithSyntax)
            {
                throw new UnsupportedLegacyProjection(projection.Name, block.GetType().Name);
            }

            if ((block is JoinSyntax join && join.Events.Any(_ => _.AutoMap == AutoMapMode.Disabled)) ||
                block is EverySyntax { AutoMap: AutoMapMode.Disabled })
            {
                throw new UnsupportedLegacyProjection(projection.Name, "block-level no automap");
            }

            if (block is FromSyntax from && from.Events.Skip(1).Any() && from.Mappings.Any())
            {
                throw new UnsupportedLegacyProjection(projection.Name, "multi-event from mappings");
            }

            if ((block is RemoveWithSyntax { Key: not null } or RemoveWithSyntax { ParentKey: not null }) ||
                block is RemoveViaJoinSyntax { Key: not null and not PathExpressionSyntax } ||
                (block is RemoveViaJoinSyntax { Key: PathExpressionSyntax key } viaJoin && !events.Declares(viaJoin.Event, key.Path)))
            {
                throw new UnsupportedLegacyProjection(projection.Name, "root removal key");
            }

            EnsureMappings(projection.Name, block);

            if (block is ChildrenSyntax children)
            {
                EnsureChild(projection.Name, children.Blocks, "children", events);
            }
            else if (block is NestedSyntax nested)
            {
                EnsureChild(projection.Name, nested.Blocks, "nested", events);
            }
            else if (block is not (FromSyntax or EverySyntax or JoinSyntax or RemoveWithSyntax or RemoveViaJoinSyntax))
            {
                throw new UnsupportedLegacyProjection(projection.Name, block.GetType().Name);
            }
        }
    }

    static void EnsureChild(string projection, IEnumerable<ProjectionBlockSyntax> blocks, string context, EventPropertyIndex events)
    {
        foreach (var block in blocks)
        {
            var unsupported = context == "children"
                ? block is JoinSyntax or AllSyntax or EverySyntax or RemoveViaJoinSyntax or ClearWithSyntax
                : block is JoinSyntax or AllSyntax or EverySyntax or RemoveWithSyntax or RemoveViaJoinSyntax;
            if (unsupported)
            {
                throw new UnsupportedLegacyProjection(projection, $"{context} {block.GetType().Name}");
            }

            if ((block is FromSyntax from && from.Events.Skip(1).Any() && from.Mappings.Any()) ||
                (block is RemoveWithSyntax removal &&
                    (removal.Key is not null and not PathExpressionSyntax || removal.ParentKey is not null and not PathExpressionSyntax ||
                        (removal.Key is PathExpressionSyntax key && !events.Declares(removal.Event, key.Path)) ||
                        (removal.ParentKey is PathExpressionSyntax parent && !events.Declares(removal.Event, parent.Path)))))
            {
                throw new UnsupportedLegacyProjection(projection, $"{context} {block.GetType().Name}");
            }

            EnsureMappings(projection, block);

            if (block is ChildrenSyntax children)
            {
                EnsureChild(projection, children.Blocks, "children", events);
            }
            else if (block is NestedSyntax nested)
            {
                EnsureChild(projection, nested.Blocks, "nested", events);
            }
            else if (block is not (FromSyntax or RemoveWithSyntax or ClearWithSyntax))
            {
                throw new UnsupportedLegacyProjection(projection, block.GetType().Name);
            }
        }
    }

    static void EnsureMappings(string projection, ProjectionBlockSyntax block)
    {
        var mappings = block switch
        {
            FromSyntax from => from.Mappings,
            EverySyntax every => every.Mappings,
            JoinSyntax join => join.Events.SelectMany(_ => _.Mappings),
            _ => []
        };
        foreach (var mapping in mappings)
        {
            if (mapping is ClearMappingSyntax or SetMappingSyntax { Source: LiteralExpressionSyntax { Value: null } } ||
                (block is EverySyntax && mapping is not SetMappingSyntax) ||
                (block is JoinSyntax && mapping is not SetMappingSyntax { Source: PathExpressionSyntax }))
            {
                throw new UnsupportedLegacyProjection(projection, mapping.GetType().Name);
            }
        }
    }
}
