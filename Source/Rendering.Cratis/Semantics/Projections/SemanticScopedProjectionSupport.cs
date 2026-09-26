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
    /// <param name="identity">The child identity established by the child key.</param>
    /// <param name="isNested">Whether this scope targets a nested object.</param>
    /// <returns>The reason, or null when supported.</returns>
    public static string? Rejection(SemanticProjectionScope scope, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool child = false, SemanticId? identity = null, bool isNested = false)
    {
        if (scope.Nested.Any(_ => _.Scope.Joins.Length > 0 || _.Scope.Children.Length > 0 || _.Scope.JoinRemovals.Length > 0))
        {
            return "Joins and children inside nested are blocked by Chronicle#4125: the engine drops their subscriptions.";
        }

        if (scope.Every is { IncludeChildren: true } && (scope.Children.Length > 0 || scope.Nested.Length > 0))
        {
            return "Every with IncludeChildren is blocked by Chronicle#4125: the engine double-applies the mappings.";
        }

        if (scope.JoinRemovals.Length > 0 && !child && !isNested)
        {
            return "Root remove via join is blocked by Chronicle#4125: the engine removes a child at the root path instead of deleting matching root instances.";
        }

        if (scope.Nested.Any(nested => nested.Scope.Removals.Length > 0))
        {
            return "Nested clear cannot render: after clear and a matching root from, Chronicle v19.8.0 cannot re-create the nested object; Chronicle#4166 remains unreleased.";
        }

        if (isNested && scope.JoinRemovals.Length > 0)
        {
            return "Join removals inside nested are blocked by Chronicle#4125.";
        }

        if (scope.Every is { Mappings.Length: > 0 } every &&
            (every.Mappings.Any(mapping => mapping.Operation != SemanticProjectionOperation.Set ||
                mapping.Source is not (SemanticProjectionEventSourceIdentity or SemanticProjectionLiteral)) ||
             !EveryMappingsSupported(every.Mappings, properties, context)))
        {
            return "Every mappings need a Chronicle fluent equivalent for each bound value and operation.";
        }

        if (scope.From.Any(from => from.Key is SemanticProjectionCompositeKey || from.ParentKey is SemanticProjectionCompositeKey) ||
            scope.Removals.Any(removal => removal.Key is SemanticProjectionCompositeKey || removal.ParentKey is SemanticProjectionCompositeKey) ||
            scope.JoinRemovals.Any(removal => removal.Key is SemanticProjectionCompositeKey))
        {
            return "Composite keys cannot render on Chronicle v19.8.0: keys containing '-', '@' or ':' are resolved incorrectly (Chronicle#4163, fixed in v19.8.1).";
        }

        if (child && identity is null)
        {
            return "A child collection needs an identified element.";
        }

        SemanticId? protectedIdentity = null;
        if (child)
        {
            protectedIdentity = identity;
        }
        else if (!isNested)
        {
            protectedIdentity = properties.Single(_ => _.IsIdentifier).Id;
        }

        if (protectedIdentity is { } protectedId &&
            (scope.Every?.Mappings.Any(mapping => mapping.Target.Contains(protectedId)) == true ||
             scope.Joins.SelectMany(join => join.Mappings).Any(mapping => mapping.Target.Contains(protectedId)) ||
             scope.From.Any(from => from.Mappings.Any(mapping => mapping.Target.Contains(protectedId) &&
                !MatchesKey(mapping, from.Key, protectedId)))))
        {
            return "Mappings cannot overwrite the key or child identity established by the projection.";
        }

        if (scope.From.Length == 0 || scope.From.Select(_ => _.EventContract).Distinct().Count() != scope.From.Length ||
            scope.Joins.Select(_ => _.EventContract).Distinct().Count() != scope.Joins.Length ||
            scope.Removals.Select(_ => _.EventContract).Distinct().Count() != scope.Removals.Length)
        {
            return "A scope must have distinct from, join, and removal event contracts.";
        }

        if (!child && !isNested && RootRoleConflict(scope, context) is { } conflict)
        {
            return conflict;
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
                !MappingsSupported(from.Mappings, properties, @event, context) ||
                !EstablishesRequiredProperties(from.Mappings, properties, context, protectedIdentity))
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

        foreach (var removal in scope.Removals)
        {
            if (!context.Events.TryGetValue(removal.EventContract, out var @event) ||
                !KeySupported(removal.Key, @event, context) ||
                (child ? removal.ParentKey is not null && !KeySupported(removal.ParentKey, @event, context) : removal.ParentKey is not null))
            {
                return "A removal has an unsupported key, parent key, or event.";
            }
        }

        foreach (var joinRemoval in scope.JoinRemovals)
        {
            if (!context.Events.TryGetValue(joinRemoval.EventContract, out var @event) ||
                !KeySupported(joinRemoval.Key, @event, context))
            {
                return "A join removal has an unsupported event or key.";
            }
        }

        foreach (var nested in scope.Nested)
        {
            var property = properties.SingleOrDefault(_ => _.Id == nested.Property);
            if (property?.Type is not { IsCollection: false, IsOptional: true, Kind: SemanticTypeReferenceKind.CompositeType } ||
                !context.Types.TryGetValue(property.Type.Target, out var composite))
            {
                return "A nested block needs an optional composite property.";
            }

            if (Rejection(nested.Scope, context, composite.Properties, isNested: true) is { } reason)
            {
                return $"A nested block cannot render: {reason}";
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

            if (Rejection(children.Scope, context, element.Properties, true, children.IdentifiedBy) is { } reason)
            {
                return $"A children block cannot render: {reason}";
            }
        }

        return null;
    }

    static string? RootRoleConflict(SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var roots = scope.From.ToDictionary(from => from.EventContract);
        var roles = scope.From.Select(from => (from.EventContract, Role: "root", from.Key))
            .Concat(scope.Joins.Select(join => (join.EventContract, Role: "join", Key: SemanticProjectionKey.EventSourceIdentity)))
            .Concat(scope.Removals.Select(removal => (removal.EventContract, Role: "removal", removal.Key)))
            .Concat(NestedRoles(scope));
        foreach (var group in roles.GroupBy(role => role.EventContract))
        {
            var occurrences = group.ToArray();
            if (occurrences.Length == 1 && occurrences[0].Role != "nested")
            {
                continue;
            }

            var name = context.Events[group.Key].Name;
            if (!roots.TryGetValue(group.Key, out var root) ||
                occurrences.Count(role => role.Role == "root") != 1 ||
                occurrences.Count(role => role.Role == "nested") != occurrences.Length - 1 ||
                occurrences.Count(role => role.Role == "nested") > 1 ||
                !occurrences.All(role => SameKey(role.Key, root.Key)))
            {
                return $"Event '{name}' is used in incompatible projection roles ({string.Join(", ", occurrences.Select(role => role.Role))}); a nested from or clear requires a root from for the same contract and identical key.";
            }
        }

        return null;
    }

    static IEnumerable<(SemanticId EventContract, string Role, SemanticProjectionKey Key)> NestedRoles(SemanticProjectionScope scope) =>
        scope.Nested.SelectMany(nested => nested.Scope.From.Select(from => (from.EventContract, Role: "nested", from.Key))
            .Concat(nested.Scope.Removals.Select(removal => (removal.EventContract, Role: "nested", removal.Key)))
            .Concat(NestedRoles(nested.Scope)));

    static bool SameKey(SemanticProjectionKey left, SemanticProjectionKey right) => (left, right) switch
    {
        (SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }, SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) => true,
        (SemanticProjectionValueKey { Value: SemanticProjectionEventProperty a }, SemanticProjectionValueKey { Value: SemanticProjectionEventProperty b }) => a.Path.SequenceEqual(b.Path),
        _ => false
    };

    static bool EveryMappingsSupported(IEnumerable<SemanticProjectionMapping> mappings, IReadOnlyList<SemanticProperty> targets, SemanticApplicationContext context) =>
        mappings.Select(mapping => string.Join('.', mapping.Target)).Distinct().Count() == mappings.Count() &&
        mappings.All(mapping => Target(mapping.Target, targets, context) is { Type.IsCollection: false } target &&
            !HasOptionalIntermediate(mapping.Target, targets, context) &&
            (target.Type.Kind == SemanticTypeReferenceKind.Concept || target.Type.Kind == SemanticTypeReferenceKind.Primitive));

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
        if (target?.Type.IsCollection != false || HasOptionalIntermediate(mapping.Target, targets, context))
        {
            return false;
        }

        if (mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract or
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement &&
            UnderlyingPrimitive(target.Type, context) == SemanticPrimitiveType.WholeNumber)
        {
            return false;
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Clear => target.Type.IsOptional && mapping.Source is null,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Set => mapping.Source is SemanticProjectionEventSourceIdentity or SemanticProjectionLiteral ||
                (mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context)),
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract =>
                mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context),
            _ => false
        };
    }

    static bool HasOptionalIntermediate(
        System.Collections.Immutable.ImmutableArray<SemanticId> path,
        IReadOnlyList<SemanticProperty> properties,
        SemanticApplicationContext context)
    {
        foreach (var id in path.SkipLast(1))
        {
            var property = properties.SingleOrDefault(candidate => candidate.Id == id);
            if (property is not { Type: { IsOptional: false, Kind: SemanticTypeReferenceKind.CompositeType } type } ||
                !context.Types.TryGetValue(type.Target, out var composite))
            {
                return true;
            }

            properties = composite.Properties;
        }

        return false;
    }

    static bool MatchesKey(SemanticProjectionMapping mapping, SemanticProjectionKey key, SemanticId identity) =>
        mapping.Target.Length == 1 && mapping.Target[0] == identity && mapping.Operation == SemanticProjectionOperation.Set &&
        key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty keyProperty } &&
        mapping.Source is SemanticProjectionEventProperty source && source.Path.SequenceEqual(keyProperty.Path);

    static SemanticPrimitiveType UnderlyingPrimitive(SemanticTypeReference type, SemanticApplicationContext context) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;

    static bool EstablishesRequiredProperties(
        IEnumerable<SemanticProjectionMapping> mappings,
        IReadOnlyList<SemanticProperty> properties,
        SemanticApplicationContext context,
        SemanticId? identity)
    {
        var paths = mappings.Where(mapping => mapping.Operation == SemanticProjectionOperation.Set)
            .Select(mapping => mapping.Target.ToArray()).ToArray();
        return Required(properties, context, [], paths, identity);
    }

    static bool Required(
        IReadOnlyList<SemanticProperty> properties,
        SemanticApplicationContext context,
        SemanticId[] prefix,
        SemanticId[][] paths,
        SemanticId? identity) =>
        properties.All(property => property.Id == identity || property.Type.IsCollection ||
            (property.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(property.Type.Target, out var composite)
                ? (property.Type.IsOptional && !paths.Any(path => path.Length > prefix.Length &&
                    path.Take(prefix.Length + 1).SequenceEqual([.. prefix, property.Id]))) ||
                  (Required(composite.Properties, context, [.. prefix, property.Id], paths, null) &&
                   paths.Any(path => path.Length > prefix.Length && path.Take(prefix.Length + 1).SequenceEqual([.. prefix, property.Id])))
                : property.Type.IsOptional || paths.Any(path => path.SequenceEqual([.. prefix, property.Id]))));

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
