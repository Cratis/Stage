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

    /// <summary>
    /// Whether the every literals of a scope are set by the every block itself (Chronicle#4663)
    /// rather than lowered onto each from block.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns>True when the literals render natively in the every block.</returns>
    public static bool RendersEveryLiteralsNatively(SemanticProjectionScope scope) =>
        scope.Every is { SubscribesToAllEvents: true } || scope.Joins.Length > 0;

    /// <summary>Gets the mappings emitted on the every/all builder, excluding join-free lowered literals.</summary>
    /// <param name="scope">The scope.</param>
    /// <returns>The mappings that remain native.</returns>
    public static IEnumerable<SemanticProjectionMapping> NativeEveryMappings(SemanticProjectionScope scope) =>
        scope.Every?.Mappings.Where(mapping => RendersEveryLiteralsNatively(scope) || mapping.Source is not SemanticProjectionLiteral) ?? [];

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
        bool keyIsEventSource,
        bool fromBlock = false) =>
        mappings.Select(_ => string.Join('.', _.Target)).Distinct().Count() == mappings.Count() &&
        mappings.All(mapping => MappingSupported(mapping, targets, @event, context, identity, keyIsEventSource, fromBlock));

    static bool MappingSupported(
        SemanticProjectionMapping mapping,
        IReadOnlyList<SemanticProperty> targets,
        SemanticEventContract @event,
        SemanticApplicationContext context,
        SemanticId? identity,
        bool keyIsEventSource,
        bool fromBlock = false)
    {
        var target = Target(mapping.Target, targets, context);
        if (target is null || HasOptionalIntermediate(mapping.Target, targets, context) ||
            (target.Type.IsCollection && !WholeCollectionSupported(mapping, target, targets, @event, context)))
        {
            return false;
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Clear => target.Type.IsOptional && mapping.Source is null,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Set => (mapping.Source is SemanticProjectionEventSourceIdentity && keyIsEventSource && EventSourceMappingSupported(mapping, targets, context, identity)) ||
                (mapping.Source is SemanticProjectionEventSourceIdentity && fromBlock && !keyIsEventSource && EventSourceIsProducedAs(@event, target.Type, context)) ||
                (mapping.Source is SemanticProjectionLiteral literal && LiteralSupported(literal, target.Type, context)) ||
                (mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context)),

            // A product is carried by the event (SemanticProjectionProducts); one that cannot be is refused with its own code and reason
            // (STAGE-ESM-050) before any projection is rendered, and the specification runner refuses every product (SemanticRunProjectionAdmission).
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract when mapping.Factor is not null => true,
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract =>
                (mapping.Source is SemanticProjectionEventProperty property && PathSupported(property.Path, @event.Properties, context)) ||
                IsZeroTotalStart(mapping, target, context),
            _ => false
        };
    }

    static bool IsArithmetic(SemanticProjectionMapping mapping) =>
        mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract or
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement;

    static bool CanSetNull(SemanticProjectionMapping mapping) =>
        mapping.Operation == SemanticProjectionOperation.Clear ||
        (mapping.Operation == SemanticProjectionOperation.Set && mapping.Source is not SemanticProjectionLiteral { Value: not SemanticNullValue });

    static SemanticProperty? EventProperty(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        SemanticProperty? found = null;
        foreach (var id in path)
        {
            found = properties.SingleOrDefault(_ => _.Id == id);
            if (found is null)
            {
                return null;
            }

            properties = ChildProperties(found, context);
        }

        return found;
    }

    // The lambda Chronicle reads is built by the C# compiler, which wraps each conversion in an expression node: Chronicle unwraps one.
    // Same type (apart from the optional marker): none. A concept into the number it holds, a number into a concept, or a concept into
    // another concept of the same number: one. A concept into an optional number: two.
    static bool SameTotalType(SemanticTypeReference source, SemanticTypeReference target, SemanticApplicationContext context)
    {
        if (source.IsCollection || target.IsCollection)
        {
            return false;
        }

        if (source.Kind == target.Kind && source.Target == target.Target && source.Primitive == target.Primitive)
        {
            return true;
        }

        var sourcePrimitive = UnderlyingPrimitive(source, context);
        if (sourcePrimitive != UnderlyingPrimitive(target, context))
        {
            return false;
        }

        // Between two concepts the generated target concept declares the implicit conversion (SemanticProjectionConversions).
        return (source.Kind == SemanticTypeReferenceKind.Concept && target.Kind == SemanticTypeReferenceKind.Primitive && !target.IsOptional) ||
            (source.Kind == SemanticTypeReferenceKind.Primitive && target.Kind == SemanticTypeReferenceKind.Concept) ||
            (source.Kind == SemanticTypeReferenceKind.Concept && target.Kind == SemanticTypeReferenceKind.Concept &&
                context.Concepts[source.Target].Values.IsEmpty && context.Concepts[target.Target].Values.IsEmpty);
    }

    // 'add total by 0' (or subtract) only makes the running total exist: the reference seeds a missing total with 0 and adds nothing, and a
    // required whole or decimal number of the Chronicle read model is 0 until the first real operation, so the renderer emits nothing for it.
    // A concept is a reference type there: it stays null until set, so a concept total does not start at 0 and is refused.
    // Chronicle's Add and Subtract builders take an event property only, so no other constant can be rendered.
    internal static bool IsZeroTotalStart(SemanticProjectionMapping mapping, SemanticProperty? target, SemanticApplicationContext context) =>
        mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract &&
        mapping.Source is SemanticProjectionLiteral { Value: SemanticNumberValue { Value: 0m } } &&
        target is { Type: { IsOptional: false, IsCollection: false } type } &&
        type.Kind == SemanticTypeReferenceKind.Primitive &&
        type.Primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;

    // A projection keyed by an event property still reads the event's source identity ($eventSourceId): Chronicle converts the identity text
    // into the target's type, which is only sound when every command that produces the event addresses it with a value of exactly that type
    // and every capture or reaction addresses it with that type as well.
    static bool EventSourceIsProducedAs(SemanticEventContract @event, SemanticTypeReference target, SemanticApplicationContext context)
    {
        var destinations = context.Commands.Values
            .SelectMany(command => command.Produces.Where(produced => produced.EventContract == @event.Id).Select(produced => (command, produced)))
            .Select(production => SemanticDestinations.Of(production.command, production.produced) is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property, Root: SemanticExpressionRootKind.Command } resolved
                ? production.command.Properties.SingleOrDefault(property => property.Id == resolved.Target)?.Type
                : null)
            .ToArray();
        var reactions = SemanticReactionProductions.In(context.Application)
            .Where(production => production.Produced.EventContract == @event.Id)
            .Select(production => production.Produced.DestinationType)
            .ToArray();
        var captures = SemanticVersionFeatures.Slices(context.Application).SelectMany(slice => slice.Captures)
            .Where(capture => capture.Appends.Any(append => append.EventContract == @event.Id) && SemanticTranslationSupport.RenderableCapture(capture, context.Events))
            .SelectMany(capture => capture.EventsSource!.Events.Select(id => context.Events[id].Properties.Single(property => property.Name == capture.Key).Type))
            .ToArray();
        return destinations.Length + reactions.Length + captures.Length > 0 &&
            captures.All(type => type is { IsCollection: false } && type with { IsOptional = false } == target with { IsOptional = false }) &&
            reactions.All(type => type is { IsCollection: false } && type with { IsOptional = false } == target with { IsOptional = false }) &&
            destinations.All(type => type is { IsCollection: false } && type with { IsOptional = false } == target with { IsOptional = false });
    }

    // A collection property is set whole from an event property of exactly the same type; elements are never addressed.
    static bool WholeCollectionSupported(SemanticProjectionMapping mapping, SemanticProperty target, IReadOnlyList<SemanticProperty> targets, SemanticEventContract @event, SemanticApplicationContext context) =>
        mapping.Operation == SemanticProjectionOperation.Set && !target.Type.IsOptional && mapping.Target.Length == 1 &&
        mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 } property &&
        @event.Properties.SingleOrDefault(candidate => candidate.Id == property.Path[0]) is { } source && source.Type == target.Type;

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

    internal static bool HasOptionalIntermediate(
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
                ? paths.Any(path => path.SequenceEqual([.. prefix, property.Id])) ||
                  (property.Type.IsOptional && !paths.Any(path => path.Length > prefix.Length &&
                    path.Take(prefix.Length + 1).SequenceEqual([.. prefix, property.Id]))) ||
                  (Required(composite.Properties, context, [.. prefix, property.Id], paths, null) &&
                   paths.Any(path => path.Length > prefix.Length && path.Take(prefix.Length + 1).SequenceEqual([.. prefix, property.Id])))
                : property.Type.IsOptional || paths.Any(path => path.SequenceEqual([.. prefix, property.Id]))));

    internal static SemanticProperty? Target(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
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

        bool IsUnverifiedEveryJoinLevel => !IsRoot && scope.Joins.Length > 0;

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
            EventNameRejection() ?? EveryRejection() ?? OptionalTotalRejection() ?? TotalTypeRejection() ?? CompositeKeyRejection() ?? ProtectedIdentityRejection() ?? RoleRejection();

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
                return "Joins inside nested remain blocked by the pinned Screenplay reference (Screenplay#1078); children and join removals inside nested are unsupported.";
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

            if (scope.Every is { SubscribesToAllEvents: true } unrelated && properties.FirstOrDefault(property =>
                !property.IsIdentifier && !unrelated.Mappings.Any(mapping => mapping.Target.Contains(property.Id)) && !NullWhenUnset(property)) is { } unset)
            {
                return $"All cannot render while '{unset.Name}' is neither mapped by all nor optional: the reference leaves it unset (null) on instances for unrelated events, but Chronicle materializes its default value.";
            }

            if (scope.Every is { IncludeChildren: true } && HasChildScopes())
            {
                return "Every with IncludeChildren is not yet verified by Stage for child and nested scopes.";
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

        // A running total that may be absent starts from nothing (Chronicle seeds a never-set property with 0), but Chronicle's Add, Subtract,
        // Increment and Decrement throw on a property that was explicitly set to null, which is what clear and a possibly absent event value do;
        // the reference then starts from 0 again. Such a total cannot render.
        string? OptionalTotalRejection()
        {
            var mappings = scope.From.SelectMany(from => from.Mappings).Concat(scope.Joins.SelectMany(join => join.Mappings)).ToArray();
            var totals = mappings.Where(mapping => IsArithmetic(mapping) && Target(mapping.Target, properties, context) is { Type.IsOptional: true })
                .Select(mapping => string.Join('/', mapping.Target)).ToHashSet(StringComparer.Ordinal);
            return mappings.Any(mapping => totals.Contains(string.Join('/', mapping.Target)) && CanSetNull(mapping))
                ? "A running total that may be absent cannot also be cleared or set from an event value that may be absent: Chronicle cannot add to a property it has set to null."
                : null;
        }

        // Chronicle reads the added event value from a direct member access and never executes the lambda, so a conversion in it fails when
        // the projection is built: the event property must have the type the total holds (a concept is not a number, another concept is not
        // the same concept), apart from the optional marker a total may carry.
        string? TotalTypeRejection()
        {
            foreach (var from in scope.From)
            {
                if (!context.Events.TryGetValue(from.EventContract, out var @event))
                {
                    continue;
                }

                foreach (var mapping in from.Mappings.Where(mapping => mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract &&
                    mapping.Factor is null && mapping.Source is SemanticProjectionEventProperty))
                {
                    var source = EventProperty(((SemanticProjectionEventProperty)mapping.Source!).Path, @event.Properties, context);
                    var target = Target(mapping.Target, properties, context);
                    if (source is not null && target is not null && !SameTotalType(source.Type, target.Type, context))
                    {
                        return $"A running total '{target.Name}' adds the event value '{source.Name}', which has another type: Chronicle adds an event property only where it has the type the total holds, so give both the same type.";
                    }
                }
            }

            return null;
        }

        string? EveryRejection()
        {
            if (scope.Every is { Mappings.Length: > 0 } every && EveryMappingsUnsupported(every))
            {
                return "Every mappings need a Chronicle fluent equivalent for each bound value and operation.";
            }

            var everyLiterals = EveryLiterals();
            if (everyLiterals.Length > 0 && IsUnverifiedEveryJoinLevel)
            {
                return "Every literals at child or nested levels with joins are not yet verified by Stage; native constants are supported only at the root.";
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

            if (!HasBlocks())
            {
                return "A scope needs a from or removal block.";
            }

            return HasDistinctContracts() ? null : "A scope must have distinct from, join, and removal event contracts.";
        }

        bool OverwritesIdentity(SemanticId protectedId) =>
            scope.Every?.Mappings.Any(mapping => mapping.Target.Contains(protectedId)) == true ||
            scope.Joins.SelectMany(join => join.Mappings).Any(mapping => mapping.Target.Contains(protectedId)) ||
            scope.From.Any(from => from.Mappings.Any(mapping => mapping.Target.Contains(protectedId) &&
                !MatchesKey(mapping, from.Key, protectedId)));

        // A root with only removals keeps the instances the read model already holds; nothing establishes them here.
        // Joins, every blocks and child scopes without a from are not admitted: their rendered behavior is unverified.
        bool HasBlocks() => scope.From.Length != 0 || (IsRoot && scope.Removals.Length != 0 && IsRemovalOnly());

        bool IsRemovalOnly() => scope.Joins.Length == 0 && scope.Every is null && !HasChildScopes();

        bool HasDistinctContracts() =>
            scope.From.Select(_ => _.EventContract).Distinct().Count() == scope.From.Length &&
            scope.Joins.Select(_ => _.EventContract).Distinct().Count() == scope.Joins.Length &&
            scope.Removals.Select(_ => _.EventContract).Distinct().Count() == scope.Removals.Length;

        string? RoleRejection()
        {
            if (IsRoot && RootRoleConflict(scope, context) is { } conflict)
            {
                return conflict;
            }

            return child && scope.Children.Length > 0
                ? "Recursive children need an exact Chronicle lowering before they can render."
                : null;
        }

        string? FromRejection()
        {
            if (scope.From.Any(from => !FromSupported(from)))
            {
                return "A from block has an unsupported key, parent key, event, or mapping.";
            }

            if (scope.From.Length == 0)
            {
                return null;
            }

            var everyLiterals = EveryLiterals();

            // A composite root key establishes every key part itself, so the parts are not left for the mappings.
            var required = IsCompositeRoot ? [.. properties.Where(property => !property.IsIdentifier)] : properties;
            var joined = child ? scope.Joins.SelectMany(join => join.Mappings) : [];
            return scope.From.Any(from => EstablishesRequiredProperties(from.Mappings.Concat(everyLiterals).Concat(joined), required, context, _protectedIdentity))
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
                from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity },
                fromBlock: true);

        bool ParentKeyUnsupported(SemanticProjectionKey? parentKey, SemanticEventContract @event) =>
            child ? parentKey is not null && !KeySupported(parentKey, @event, context) : parentKey is not null;

        string? JoinRejection() =>
            scope.Joins.Any(join => !JoinSupported(join)) ? "A join has an unsupported correlation key or mapping." : null;

        bool JoinSupported(SemanticProjectionJoin join) =>
            context.Events.TryGetValue(join.EventContract, out var @event) && join.Key is null &&
            properties.Any(_ => _.Id == join.On) && (!child || join.On == identity) && MappingsSupported(join.Mappings, properties, @event, context, _protectedIdentity, false);

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
