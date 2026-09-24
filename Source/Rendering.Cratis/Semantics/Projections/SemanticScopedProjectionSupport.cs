// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Identifies the scoped projection subset that can be lowered without changing its meaning.
/// </summary>
internal static class SemanticScopedProjectionSupport
{
    /// <summary>
    /// Returns the first reason a scope cannot be represented by the emitted Chronicle projection.
    /// </summary>
    /// <param name="scope">The scope to inspect.</param>
    /// <param name="context">The semantic application.</param>
    /// <param name="properties">The properties at this level.</param>
    /// <param name="child">Whether this is a child collection.</param>
    /// <returns>The reason, or null when supported.</returns>
    public static string? Rejection(SemanticProjectionScope scope, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool child = false)
    {
        if (scope.From.SelectMany(_ => _.Mappings).Concat(scope.Joins.SelectMany(_ => _.Mappings))
            .Concat(scope.Every?.Mappings ?? []).Any(_ => _.Source is SemanticProjectionLiteral))
        {
            return "Literal mappings are blocked by Chronicle#4124: the engine may resolve their values as null.";
        }

        if (scope.Nested.Any(_ => _.Scope.Joins.Length > 0 || _.Scope.Children.Length > 0 || _.Scope.JoinRemovals.Length > 0))
        {
            return "Joins and children inside nested are blocked by Chronicle#4125: the engine drops their subscriptions.";
        }

        if (scope.Every is { IncludeChildren: true } && (scope.Children.Length > 0 || scope.Nested.Length > 0))
        {
            return "Every with IncludeChildren is blocked by Chronicle#4125: the engine double-applies the mappings.";
        }

        if (scope.Every is not null || scope.JoinRemovals.Length > 0 || scope.Nested.Length > 0 || scope.Removals.Length > 0)
        {
            return "Nested, every/all, and removal blocks need an exact Chronicle lowering before they can render.";
        }

        if (scope.From.Length == 0 || scope.From.Select(_ => _.EventContract).Distinct().Count() != scope.From.Length ||
            scope.Joins.Select(_ => _.EventContract).Distinct().Count() != scope.Joins.Length)
        {
            return "A scope must have distinct from and join event contracts.";
        }

        if (child && (scope.Children.Length > 0 || scope.Joins.Length > 0))
        {
            return "Recursive children and child joins need an exact Chronicle lowering before they can render.";
        }

        foreach (var from in scope.From)
        {
            if (!context.Events.TryGetValue(from.EventContract, out var @event) ||
                !KeySupported(from.Key, @event, context) ||
                (child ? from.ParentKey is not null && !KeySupported(from.ParentKey, @event, context) : from.ParentKey is not null) ||
                !MappingsSupported(from.Mappings, properties, @event, context))
            {
                return "A from block has an unsupported key, parent key, event, or mapping.";
            }
        }

        foreach (var join in scope.Joins)
        {
            if (!context.Events.TryGetValue(join.EventContract, out var @event) || join.Key is not null ||
                !properties.Any(_ => _.Id == join.On) || !MappingsSupported(join.Mappings, properties, @event, context))
            {
                return "A join has an unsupported correlation key or mapping.";
            }
        }

        foreach (var children in scope.Children)
        {
            var property = properties.SingleOrDefault(_ => _.Id == children.Property);
            if (property?.Type.IsCollection != true || property.Type.Kind != SemanticTypeReferenceKind.CompositeType ||
                !context.Types.TryGetValue(property.Type.Target, out var element) ||
                !element.Properties.Any(_ => _.Id == children.IdentifiedBy))
            {
                return "A children block has an unsupported collection or identity.";
            }

            if (Rejection(children.Scope, context, element.Properties, true) is { } reason)
            {
                return $"A children block cannot render: {reason}";
            }
        }

        return null;
    }

    static bool KeySupported(SemanticProjectionKey key, SemanticEventContract @event, SemanticApplicationContext context) =>
        key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } ||
        (key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } && PathSupported(property.Path, @event.Properties, context));

    static bool MappingsSupported(
        IEnumerable<SemanticProjectionMapping> mappings,
        IReadOnlyList<SemanticProperty> targets,
        SemanticEventContract @event,
        SemanticApplicationContext context) =>
        mappings.Select(_ => string.Join('.', _.Target)).Distinct().Count() == mappings.Count() &&
        mappings.All(mapping => MappingSupported(mapping, targets, @event, context));

    static bool MappingSupported(
        SemanticProjectionMapping mapping,
        IReadOnlyList<SemanticProperty> targets,
        SemanticEventContract @event,
        SemanticApplicationContext context)
    {
        var target = Target(mapping.Target, targets, context);
        if (target?.Type.IsCollection != false)
        {
            return false;
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Clear => target.Type.IsOptional && mapping.Source is null,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Set => mapping.Source is SemanticProjectionEventSourceIdentity ||
                (mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context)),
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract =>
                mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context),
            _ => false
        };
    }

    static SemanticProperty? Target(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        SemanticProperty? target = null;
        foreach (var id in path)
        {
            target = properties.SingleOrDefault(_ => _.Id == id);
            if (target is null)
            {
                return null;
            }

            properties = target.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(target.Type.Target, out var type)
                ? type.Properties : [];
        }

        return target;
    }

    static bool PathSupported(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        if (path.IsEmpty)
        {
            return false;
        }

        foreach (var id in path)
        {
            var property = properties.SingleOrDefault(_ => _.Id == id);
            if (property is null)
            {
                return false;
            }

            properties = property.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(property.Type.Target, out var type)
                ? type.Properties : [];
        }

        return true;
    }
}
