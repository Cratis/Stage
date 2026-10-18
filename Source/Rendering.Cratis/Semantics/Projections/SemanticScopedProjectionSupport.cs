// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

using Cratis.Screenplay.Semantics;

#pragma warning disable MA0110, SA1202 // The current SDK does not generate implementations for these regex members in this project.
namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Identifies the scoped projection subset that can be lowered without changing its meaning.
/// </summary>
internal static class SemanticScopedProjectionSupport
{
    static readonly Regex _fluentTextLiteral = new(@"\A[\w ._/:*+-]*\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(1000));

    static Regex FluentTextLiteral() => _fluentTextLiteral;

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
    public static string? Rejection(SemanticProjectionScope scope, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool child = false, SemanticId? identity = null, bool isNested = false) =>
        new ScopeRejection(scope, context, properties, child, identity, isNested).Find();

    static string? RootRoleConflict(SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var roots = scope.From.ToDictionary(from => from.EventContract);
        var roles = scope.From.Select(from => (from.EventContract, Role: "root", from.Key))
            .Concat(scope.Joins.Select(join => (join.EventContract, Role: "join", Key: SemanticProjectionKey.EventSourceIdentity)))
            .Concat(scope.Removals.Select(removal => (removal.EventContract, Role: "removal", removal.Key)))
            .Concat(NestedRoles(scope));
        foreach (var occurrences in roles.GroupBy(role => role.EventContract).Select(group => group.ToArray()).Where(NeedsRoleCheck))
        {
            var name = context.Events[occurrences[0].EventContract].Name;
            if (!roots.TryGetValue(occurrences[0].EventContract, out var root) || !RolesCompatible(occurrences, root))
            {
                return $"Event '{name}' is used in incompatible projection roles ({string.Join(", ", occurrences.Select(role => role.Role))}); a nested from or clear requires a root from for the same contract and identical key.";
            }
        }

        return null;
    }

    static bool NeedsRoleCheck((SemanticId EventContract, string Role, SemanticProjectionKey Key)[] occurrences) =>
        !(occurrences.Length == 1 && occurrences[0].Role != "nested");

    static bool RolesCompatible((SemanticId EventContract, string Role, SemanticProjectionKey Key)[] occurrences, SemanticProjectionFrom root)
    {
        var nested = occurrences.Count(role => role.Role == "nested");
        return occurrences.Count(role => role.Role == "root") == 1 &&
            nested == occurrences.Length - 1 &&
            nested <= 1 &&
            occurrences.All(role => SameKey(role.Key, root.Key));
    }

    static IEnumerable<(SemanticId EventContract, string Role, SemanticProjectionKey Key)> NestedRoles(SemanticProjectionScope scope) =>
        scope.Nested.SelectMany(nested => nested.Scope.From.Select(from => (from.EventContract, Role: "nested", from.Key))
            .Concat(nested.Scope.Removals.Select(removal => (removal.EventContract, Role: "nested", removal.Key)))
            .Concat(NestedRoles(nested.Scope)));

    static bool SameKey(SemanticProjectionKey left, SemanticProjectionKey right) => (left, right) switch
    {
        (SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }, SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) => true,
        (SemanticProjectionValueKey { Value: SemanticProjectionEventProperty a }, SemanticProjectionValueKey { Value: SemanticProjectionEventProperty b }) => a.Path.SequenceEqual(b.Path),
        (SemanticProjectionPartsKey a, SemanticProjectionPartsKey b) => a.Parts.Length == b.Parts.Length && a.Parts.All(part => b.Parts.Any(other =>
            other.Property == part.Property && part.Value is SemanticProjectionEventProperty left && other.Value is SemanticProjectionEventProperty right && left.Path.SequenceEqual(right.Path))),
        _ => false
    };

    static bool EveryMappingsSupported(IEnumerable<SemanticProjectionMapping> mappings, IReadOnlyList<SemanticProperty> targets, SemanticApplicationContext context, SemanticId? identity) =>
        mappings.Select(mapping => string.Join('.', mapping.Target)).Distinct().Count() == mappings.Count() &&
        mappings.All(mapping => Target(mapping.Target, targets, context) is { Type.IsCollection: false } target &&
            !HasOptionalIntermediate(mapping.Target, targets, context) &&
            (target.Type.Kind == SemanticTypeReferenceKind.Concept || target.Type.Kind == SemanticTypeReferenceKind.Primitive) &&
            ((mapping.Source is SemanticProjectionLiteral literal && LiteralSupported(literal, target.Type, context)) ||
             EventSourceMappingSupported(mapping, targets, context, identity)));

    // The one instance a root block always addresses: Chronicle's UsingConstantKey names it with text, which its expression resolver
    // reads unquoted, so only the characters a fluent text literal can carry are admitted.
    internal static bool ConstantKeySupported(SemanticProjectionKey key) =>
        key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue text } } &&
        text.Value.Length > 0 && FluentTextLiteral().IsMatch(text.Value);

    static bool KeySupported(SemanticProjectionKey key, SemanticEventContract @event, SemanticApplicationContext context) =>
        key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } ||
        (key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } && PathSupported(property.Path, @event.Properties, context));

    static bool MappingsSupported(
        IEnumerable<SemanticProjectionMapping> mappings,
        IReadOnlyList<SemanticProperty> targets,
        SemanticEventContract @event,
        SemanticApplicationContext context,
        SemanticId? identity,
        bool keyIsEventSource) =>
        mappings.Select(_ => string.Join('.', _.Target)).Distinct().Count() == mappings.Count() &&
        mappings.All(mapping => MappingSupported(mapping, targets, @event, context, identity, keyIsEventSource));

    static bool MappingSupported(
        SemanticProjectionMapping mapping,
        IReadOnlyList<SemanticProperty> targets,
        SemanticEventContract @event,
        SemanticApplicationContext context,
        SemanticId? identity,
        bool keyIsEventSource)
    {
        var target = Target(mapping.Target, targets, context);
        if (target?.Type.IsCollection != false || HasOptionalIntermediate(mapping.Target, targets, context))
        {
            return false;
        }

        // A running whole-number total that may be absent has no zero to start from: only a required count or sum accumulates from nothing.
        if (mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract or
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement &&
            target.Type.IsOptional && UnderlyingPrimitive(target.Type, context) == SemanticPrimitiveType.WholeNumber)
        {
            return false;
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Clear => target.Type.IsOptional && mapping.Source is null,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Set => (mapping.Source is SemanticProjectionEventSourceIdentity && keyIsEventSource && EventSourceMappingSupported(mapping, targets, context, identity)) ||
                (mapping.Source is SemanticProjectionLiteral literal && LiteralSupported(literal, target.Type, context)) ||
                (mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context)),
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract =>
                mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context),
            _ => false
        };
    }

    // Chronicle's source identity becomes text in the runner. Only an unconstrained target of the
    // exact identifier type can be reconstructed without bypassing Screenplay's value validation.
    static bool EventSourceMappingSupported(
        SemanticProjectionMapping mapping,
        IReadOnlyList<SemanticProperty> properties,
        SemanticApplicationContext context,
        SemanticId? identity)
    {
        if (identity is null || mapping.Target.Length != 1)
        {
            return false;
        }

        var identifier = properties.SingleOrDefault(property => property.Id == identity);
        var target = properties.SingleOrDefault(property => property.Id == mapping.Target[0]);
        if (identifier is null || target is null)
        {
            return false;
        }

        var targetType = target.Type with { IsOptional = false };
        var identifierType = identifier.Type with { IsOptional = false };
        return target.Id == identifier.Id ||
            (targetType == identifierType &&
             (target.Type.Kind != SemanticTypeReferenceKind.Concept || context.Concepts[target.Type.Target].Values.IsEmpty));
    }

    // Chronicle's fluent ToValue embeds text in an unquoted $value(...) expression. Its resolver is
    // unanchored, so a closing parenthesis may silently truncate the value before projection.
    static bool LiteralSupported(SemanticProjectionLiteral literal, SemanticTypeReference target, SemanticApplicationContext context)
    {
        var primitive = target.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[target.Target].Primitive : target.Primitive;
        return primitive != SemanticPrimitiveType.Text || (literal.Value is SemanticTextValue text && FluentTextLiteral().IsMatch(text.Value));
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
        key switch
        {
            SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } => mapping.Source is SemanticProjectionEventSourceIdentity,
            SemanticProjectionValueKey { Value: SemanticProjectionEventProperty keyProperty } => mapping.Source is SemanticProjectionEventProperty source && source.Path.SequenceEqual(keyProperty.Path),
            SemanticProjectionValueKey { Value: SemanticProjectionLiteral keyLiteral } => mapping.Source is SemanticProjectionLiteral source && Equals(source.Value, keyLiteral.Value),
            _ => false
        };

    static SemanticPrimitiveType UnderlyingPrimitive(SemanticTypeReference type, SemanticApplicationContext context) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;

    static bool EstablishesRequiredProperties(
        IEnumerable<SemanticProjectionMapping> mappings,
        IReadOnlyList<SemanticProperty> properties,
        SemanticApplicationContext context,
        SemanticId? identity)
    {
        // A running total starts at zero where nothing was set: the first fact that touches it is what establishes it.
        var paths = mappings.Where(mapping => mapping.Operation is SemanticProjectionOperation.Set or SemanticProjectionOperation.Add or
                SemanticProjectionOperation.Subtract or SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement)
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

            properties = ChildProperties(target, context);
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
            if (property is null || !ChronicleEventPropertyNameIsSafe.Check(property.Name))
            {
                return false;
            }

            properties = ChildProperties(property, context);
        }

        return true;
    }

    static System.Collections.Immutable.ImmutableArray<SemanticProperty> ChildProperties(SemanticProperty property, SemanticApplicationContext context) =>
        property.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(property.Type.Target, out var type)
            ? type.Properties : [];

    sealed class ScopeRejection(
        SemanticProjectionScope scope,
        SemanticApplicationContext context,
        IReadOnlyList<SemanticProperty> properties,
        bool child,
        SemanticId? identity,
        bool isNested)
    {
        SemanticId? _protectedIdentity;

        bool IsRoot => !child && !isNested;

        bool IsCompositeRoot => IsRoot && properties.Count(property => property.IsIdentifier) > 1;

        static bool SharesTargetPrefix(SemanticProjectionMapping mapping, SemanticProjectionMapping literal)
        {
            var length = Math.Min(mapping.Target.Length, literal.Target.Length);
            return mapping.Target.Take(length).SequenceEqual(literal.Target.Take(length));
        }

        static bool IsComposite(SemanticProjectionKey? key) => key is SemanticProjectionCompositeKey;

        static bool IsBlockedInNested(SemanticProjectionNested nested) =>
            nested.Scope.Joins.Length > 0 || nested.Scope.Children.Length > 0 || nested.Scope.JoinRemovals.Length > 0;

        static bool IsCollectionOfComposites(SemanticProperty? property) =>
            property?.Type.IsCollection == true && property.Type.Kind == SemanticTypeReferenceKind.CompositeType;

        public string? Find()
        {
            var limit = LimitRejection();
            if (limit is not null)
            {
                return limit;
            }

            _protectedIdentity = ProtectedIdentity();
            return MappingRejection() ?? BlockRejection();
        }

        string? LimitRejection() => ChronicleLimitRejection() ?? EveryLimitRejection() ?? JoinRemovalRejection();

        string? MappingRejection() =>
            EventNameRejection() ?? EveryRejection() ?? CompositeKeyRejection() ?? ProtectedIdentityRejection() ?? RoleRejection();

        string? BlockRejection() =>
            FromRejection() ?? JoinRejection() ?? RemovalRejection() ?? NestedRejection() ?? ChildrenRejection();

        SemanticId? ProtectedIdentity()
        {
            if (child)
            {
                return identity;
            }

            return isNested || IsCompositeRoot ? null : properties.Single(_ => _.IsIdentifier).Id;
        }

        string? ChronicleLimitRejection()
        {
            if (!ChronicleReadModelPropertyNamesAreSafe.Check(properties, rootLevel: IsRoot))
            {
                return "A read-model property name collides with Chronicle's property paths or another generated property.";
            }

            if (scope.Nested.Any(IsBlockedInNested))
            {
                return "Joins and children inside nested are blocked by Chronicle#4125: the engine drops their subscriptions.";
            }

            return null;
        }

        // Chronicle leaves a property it never sets null only where the C# type is a reference type: optional
        // values, text (including text concepts) and single composite objects. Value types, enumerations and
        // collections get a non-null default.
        bool NullWhenUnset(SemanticProperty property) =>
            property.Type.IsOptional ||
            (!property.Type.IsCollection && (property.Type.Kind == SemanticTypeReferenceKind.CompositeType ||
                (property.Type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept &&
                    UnderlyingPrimitive(property.Type, context) == SemanticPrimitiveType.Text &&
                    !(property.Type.Kind == SemanticTypeReferenceKind.Concept && context.Concepts[property.Type.Target].Values.Length > 0))));

        string? EveryLimitRejection()
        {
            if (scope.Every is { SubscribesToAllEvents: true, Mappings.Length: 0 })
            {
                return "An all block without mappings cannot render: Chronicle's FromAll subscribes only through a mapping, so the reference's instances for unrelated event sources would not exist.";
            }

            if (scope.Every is { SubscribesToAllEvents: true } all && all.Mappings.Any(mapping => mapping.Source is SemanticProjectionLiteral))
            {
                return "Literal mappings in all cannot render: Chronicle's FromAll has no native constant (Chronicle#4663), and lowering literals onto from blocks would miss events from unrelated types and sources.";
            }

            if (scope.Every is { SubscribesToAllEvents: true } unrelated && properties.FirstOrDefault(property =>
                !property.IsIdentifier && !unrelated.Mappings.Any(mapping => mapping.Target.Contains(property.Id)) && !NullWhenUnset(property)) is { } unset)
            {
                return $"All cannot render while '{unset.Name}' is neither mapped by all nor optional: the reference leaves it unset (null) on instances for unrelated events, but Chronicle materializes its default value.";
            }

            if (scope.Every is { IncludeChildren: true } && HasChildScopes())
            {
                return "Every with IncludeChildren is blocked by Chronicle#4125: the engine double-applies the mappings.";
            }

            return null;
        }

        bool HasChildScopes() => scope.Children.Length > 0 || scope.Nested.Length > 0;

        bool HasCompositeKey() =>
            scope.From.Any(from => IsComposite(from.Key) || IsComposite(from.ParentKey)) ||
            scope.Removals.Any(removal => IsComposite(removal.Key) || IsComposite(removal.ParentKey)) ||
            scope.JoinRemovals.Any(removal => IsComposite(removal.Key));

        string? JoinRemovalRejection()
        {
            if (scope.JoinRemovals.Length > 0 && IsRoot)
            {
                return "Root remove via join is blocked by https://github.com/Cratis/Chronicle/issues/4263 and https://github.com/Cratis/Screenplay/issues/563: deleting matching root instances has no verified runtime or reference semantics.";
            }

            if (isNested && scope.JoinRemovals.Length > 0)
            {
                return "Join removals inside nested are blocked by Chronicle#4125.";
            }

            return null;
        }

        string? EventNameRejection() =>
            scope.From.Select(from => from.EventContract).Concat(scope.Joins.Select(join => join.EventContract))
                .Concat(scope.Removals.Select(removal => removal.EventContract))
                .Concat(scope.JoinRemovals.Select(removal => removal.EventContract))
                .Any(HasUnsafeEventPropertyName)
                ? "An event property name collides with Chronicle expression syntax."
                : null;

        bool HasUnsafeEventPropertyName(SemanticId id) =>
            context.Events.TryGetValue(id, out var @event) &&
            @event.Properties.Any(property => !ChronicleEventPropertyNameIsSafe.Check(property.Name));

        SemanticProjectionMapping[] EveryLiterals() =>
            scope.Every?.Mappings.Where(mapping => mapping.Source is SemanticProjectionLiteral).ToArray() ?? [];

        string? EveryRejection()
        {
            if (scope.Every is { Mappings.Length: > 0 } every && EveryMappingsUnsupported(every))
            {
                return "Every mappings need a Chronicle fluent equivalent for each bound value and operation.";
            }

            var everyLiterals = EveryLiterals();
            if (everyLiterals.Length > 0 && scope.Joins.Length > 0)
            {
                return "Every literals at a level with joins need https://github.com/Cratis/Chronicle/issues/4663: lowering them onto from blocks can change join backfill eligibility and values.";
            }

            if (everyLiterals.Any(CollidesWithFromMapping))
            {
                // Only an exact Set can be replaced. Prefix overlaps and intermediate operations
                // need an exact lowering before the every literal can render.
                return "An every literal collides with an overlapping or non-Set from mapping; only an exact Set target can be replaced.";
            }

            return null;
        }

        bool EveryMappingsUnsupported(SemanticProjectionEvery every) =>
            every.Mappings.Any(mapping => mapping.Operation != SemanticProjectionOperation.Set ||
                mapping.Source is not SemanticProjectionEventSourceIdentity and not SemanticProjectionLiteral) ||
            !EveryMappingsSupported(every.Mappings, properties, context, _protectedIdentity);

        bool CollidesWithFromMapping(SemanticProjectionMapping literal) =>
            scope.From.SelectMany(from => from.Mappings).Any(mapping =>
                SharesTargetPrefix(mapping, literal) &&
                (mapping.Target.Length != literal.Target.Length || mapping.Operation != SemanticProjectionOperation.Set));

        string? CompositeKeyRejection()
        {
            if (HasCompositeKey())
            {
                return "Composite-key read models need a keyed lookup by composite key that Stage cannot generate yet; Chronicle#4265 remains open, so Stage cannot look up those instances by their composite key.";
            }

            return child && identity is null ? "A child collection needs an identified element." : null;
        }

        string? ProtectedIdentityRejection()
        {
            if (_protectedIdentity is { } protectedId && OverwritesIdentity(protectedId))
            {
                return "Mappings cannot overwrite the key or child identity established by the projection.";
            }

            return HasDistinctContracts() ? null : "A scope must have distinct from, join, and removal event contracts.";
        }

        bool OverwritesIdentity(SemanticId protectedId) =>
            scope.Every?.Mappings.Any(mapping => mapping.Target.Contains(protectedId)) == true ||
            scope.Joins.SelectMany(join => join.Mappings).Any(mapping => mapping.Target.Contains(protectedId)) ||
            scope.From.Any(from => from.Mappings.Any(mapping => mapping.Target.Contains(protectedId) &&
                !MatchesKey(mapping, from.Key, protectedId)));

        bool HasDistinctContracts() =>
            scope.From.Length != 0 &&
            scope.From.Select(_ => _.EventContract).Distinct().Count() == scope.From.Length &&
            scope.Joins.Select(_ => _.EventContract).Distinct().Count() == scope.Joins.Length &&
            scope.Removals.Select(_ => _.EventContract).Distinct().Count() == scope.Removals.Length;

        string? RoleRejection()
        {
            if (IsRoot && RootRoleConflict(scope, context) is { } conflict)
            {
                return conflict;
            }

            return child && (scope.Children.Length > 0 || scope.Joins.Length > 0)
                ? "Recursive children and child joins need an exact Chronicle lowering before they can render."
                : null;
        }

        string? FromRejection()
        {
            if (scope.From.Any(from => !FromSupported(from)))
            {
                return "A from block has an unsupported key, parent key, event, or mapping.";
            }

            var everyLiterals = EveryLiterals();

            // A composite root key establishes every key part itself, so the parts are not left for the mappings.
            var required = IsCompositeRoot ? [.. properties.Where(property => !property.IsIdentifier)] : properties;
            return scope.From.Any(from => EstablishesRequiredProperties(from.Mappings.Concat(everyLiterals), required, context, _protectedIdentity))
                ? null
                : "At least one from block must establish the required read-model properties.";
        }

        bool FromSupported(SemanticProjectionFrom from) =>
            context.Events.TryGetValue(from.EventContract, out var @event) &&
            (KeySupported(from.Key, @event, context) || (IsRoot && ConstantKeySupported(from.Key)) || PartsKeySupported(from.Key, @event)) &&
            !ParentKeyUnsupported(from.ParentKey, @event) &&
            MappingsSupported(
                from.Mappings,
                properties,
                @event,
                context,
                _protectedIdentity,
                from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity });

        bool ParentKeyUnsupported(SemanticProjectionKey? parentKey, SemanticEventContract @event) =>
            child ? parentKey is not null && !KeySupported(parentKey, @event, context) : parentKey is not null;

        string? JoinRejection() =>
            scope.Joins.Any(join => !JoinSupported(join)) ? "A join has an unsupported correlation key or mapping." : null;

        bool JoinSupported(SemanticProjectionJoin join) =>
            context.Events.TryGetValue(join.EventContract, out var @event) && join.Key is null &&
            properties.Any(_ => _.Id == join.On) && MappingsSupported(join.Mappings, properties, @event, context, _protectedIdentity, false);

        string? RemovalRejection()
        {
            if (scope.Removals.Any(removal => !RemovalSupported(removal)))
            {
                return "A removal has an unsupported key, parent key, or event.";
            }

            return scope.JoinRemovals.Any(joinRemoval => !JoinRemovalSupported(joinRemoval))
                ? "A join removal has an unsupported event or key."
                : null;
        }

        bool RemovalSupported(SemanticProjectionRemoval removal) =>
            context.Events.TryGetValue(removal.EventContract, out var @event) &&
            (KeySupported(removal.Key, @event, context) || PartsKeySupported(removal.Key, @event)) &&
            !ParentKeyUnsupported(removal.ParentKey, @event);

        bool PartsKeySupported(SemanticProjectionKey key, SemanticEventContract @event) =>
            IsCompositeRoot && key is SemanticProjectionPartsKey parts &&
            parts.Parts.All(part => part.Value is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context));

        bool JoinRemovalSupported(SemanticProjectionJoinRemoval joinRemoval) =>
            context.Events.TryGetValue(joinRemoval.EventContract, out var @event) &&
            KeySupported(joinRemoval.Key, @event, context);

        string? NestedRejection() =>
            scope.Nested.Select(NestedBlockRejection).FirstOrDefault(reason => reason is not null);

        string? NestedBlockRejection(SemanticProjectionNested nested)
        {
            var property = properties.SingleOrDefault(_ => _.Id == nested.Property);
            if (property?.Type is not { IsCollection: false, IsOptional: true, Kind: SemanticTypeReferenceKind.CompositeType } ||
                !context.Types.TryGetValue(property.Type.Target, out var composite))
            {
                return "A nested block needs an optional composite property.";
            }

            return Rejection(nested.Scope, context, composite.Properties, isNested: true) is { } reason
                ? $"A nested block cannot render: {reason}"
                : null;
        }

        string? ChildrenRejection() =>
            scope.Children.Select(ChildrenBlockRejection).FirstOrDefault(reason => reason is not null);

        string? ChildrenBlockRejection(SemanticProjectionChildren children)
        {
            var property = properties.SingleOrDefault(_ => _.Id == children.Property);
            var element = IsCollectionOfComposites(property) && context.Types.TryGetValue(property!.Type.Target, out var found) ? found : null;
            if (element?.Properties.Any(_ => _.Id == children.IdentifiedBy) != true)
            {
                return "A children block has an unsupported collection or identity.";
            }

            return Rejection(children.Scope, context, element.Properties, true, children.IdentifiedBy) is { } reason
                ? $"A children block cannot render: {reason}"
                : null;
        }
    }
}
