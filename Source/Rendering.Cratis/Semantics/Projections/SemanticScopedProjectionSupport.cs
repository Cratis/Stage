// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Cratis.Screenplay.Semantics;

#pragma warning disable MA0110, SA1202 // The current SDK does not generate implementations for these regex members in this project.
namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Identifies the scoped projection subset that can be lowered without changing its meaning.
/// </summary>
internal static partial class SemanticScopedProjectionSupport
{
    static readonly Regex _fluentTextLiteral = new(@"\A[\w ._/:*+-]*\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(1000));
    static readonly ImmutableDictionary<string, (string Property, ImmutableHashSet<SemanticPrimitiveType> Types)> _eventContextMembers =
        ImmutableDictionary.Create<string, (string Property, ImmutableHashSet<SemanticPrimitiveType> Types)>(StringComparer.Ordinal)
            .Add("occurred", ("Occurred", ImmutableHashSet.Create(SemanticPrimitiveType.DateTime)))
            .Add("correlationId", ("CorrelationId", ImmutableHashSet.Create(SemanticPrimitiveType.Uuid)))
            .Add("sequenceNumber", ("SequenceNumber", ImmutableHashSet.Create(SemanticPrimitiveType.WholeNumber)))
            .Add("eventSourceId", ("EventSourceId", ImmutableHashSet.Create(SemanticPrimitiveType.Text)));

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
        scope.Every is { SubscribesToAllEvents: true } || scope.Joins.Length > 0 ||
        (scope.Every?.Mappings.Any(mapping => mapping.Source is SemanticProjectionEventContextValue) ?? false);

    /// <summary>Gets the mappings emitted on the every/all builder, excluding join-free lowered literals.</summary>
    /// <param name="scope">The scope.</param>
    /// <returns>The mappings that remain native.</returns>
    public static IEnumerable<SemanticProjectionMapping> NativeEveryMappings(SemanticProjectionScope scope) =>
        scope.Every?.Mappings.Where(mapping => RendersEveryLiteralsNatively(scope) || mapping.Source is not SemanticProjectionLiteral) ?? [];

    static string? RootRoleConflict(SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var roots = scope.From.ToDictionary(from => from.EventContract);
        var roles = scope.From.Select(from => (from.EventContract, Role: "root", from.Key))
            .Concat(scope.Joins.Select(join => (join.EventContract, Role: "join", Key: join.Key ?? SemanticProjectionKey.EventSourceIdentity)))
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
        mappings.All(mapping => EveryMappingSupported(mapping, targets, context, identity));

    static bool EveryMappingSupported(SemanticProjectionMapping mapping, IReadOnlyList<SemanticProperty> targets, SemanticApplicationContext context, SemanticId? identity) =>
        EveryMappingTarget(mapping, targets, context) is { } target && mapping.Source switch
        {
            SemanticProjectionLiteral literal => LiteralSupported(literal, target.Type, context),
            SemanticProjectionEventContextValue value => EventContextMappingSupported(mapping, value, targets, context),
            SemanticProjectionEventSourceIdentity => EventSourceMappingSupported(mapping, targets, context, identity),
            _ => false
        };

    static SemanticProperty? EveryMappingTarget(SemanticProjectionMapping mapping, IReadOnlyList<SemanticProperty> targets, SemanticApplicationContext context) =>
        Target(mapping.Target, targets, context) is { Type.IsCollection: false } target &&
        !HasOptionalIntermediate(mapping.Target, targets, context) &&
        target.Type.Kind is SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.Primitive ? target : null;

    internal static string EventContextProperty(SemanticProjectionEventContextValue value) =>
        _eventContextMembers.TryGetValue(value.Path, out var member) ? member.Property :
            throw new InvalidOperationException($"Unsupported event-context path '{value.Path}'.");

    internal static bool EventContextMappingSupported(
        SemanticProjectionMapping mapping,
        SemanticProjectionEventContextValue value,
        IReadOnlyList<SemanticProperty> properties,
        SemanticApplicationContext context) =>
        mapping.Operation == SemanticProjectionOperation.Set &&
        !HasOptionalIntermediate(mapping.Target, properties, context) &&
        Target(mapping.Target, properties, context) is { Type.IsCollection: false } target &&
        _eventContextMembers.TryGetValue(value.Path, out var member) &&
        EventContextTypeSupported(target.Type, member.Types, context);

    static bool EventContextTypeSupported(SemanticTypeReference type, ImmutableHashSet<SemanticPrimitiveType> types, SemanticApplicationContext context) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => types.Contains(type.Primitive),
        SemanticTypeReferenceKind.Concept => context.Concepts[type.Target].Values.IsEmpty && types.Contains(context.Concepts[type.Target].Primitive),
        _ => false
    };

    // The one instance a root block always addresses: Chronicle's UsingConstantKey names it with text, which its expression resolver
    // reads unquoted, so only the characters a fluent text literal can carry are admitted.
    internal static bool ConstantKeySupported(SemanticProjectionKey key) =>
        key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue text } } &&
        text.Value.Length > 0 && FluentTextLiteral().IsMatch(text.Value);

    /// <summary>
    /// Admits default joins and required, type-matched scalar event-property keys on root joins.
    /// </summary>
    /// <param name="key">The join's correlation key.</param>
    /// <param name="on">The joined read-model property.</param>
    /// <param name="event">The joined event contract.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>Whether Chronicle can preserve the join key.</returns>
    internal static bool RootJoinKeySupported(SemanticProjectionKey? key, SemanticProperty on, SemanticEventContract @event, SemanticApplicationContext context) =>
        key is null or SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } ||
        (key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } property } &&
         @event.Properties.SingleOrDefault(candidate => candidate.Id == id) is { Type: { IsOptional: false, IsCollection: false } type } &&
         type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept &&
         type == on.Type with { IsOptional = false } && PathSupported(property.Path, @event.Properties, context));

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

        // A set-once mapping keeps the first value it receives, so it takes exactly the sources a plain set takes.
        if (mapping.Operation == SemanticProjectionOperation.SetOnce)
        {
            return mapping.Source is not SemanticProjectionEventSourceIdentity && !target.Type.IsCollection &&
                MappingSupported(mapping with { Operation = SemanticProjectionOperation.Set }, targets, @event, context, identity, keyIsEventSource, fromBlock);
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Clear => target.Type.IsOptional && mapping.Source is null,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Set => (mapping.Source is SemanticProjectionEventSourceIdentity && keyIsEventSource && EventSourceMappingSupported(mapping, targets, context, identity)) ||
                (mapping.Source is SemanticProjectionEventSourceIdentity && fromBlock && !keyIsEventSource && EventSourceIsProducedAs(@event, target.Type, context)) ||
                (mapping.Source is SemanticProjectionLiteral literal && LiteralSupported(literal, target.Type, context)) ||
                (mapping.Source is SemanticProjectionEventContextValue value && EventContextMappingSupported(mapping, value, targets, context)) ||
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

        return UnderlyingPrimitive(source, context) == UnderlyingPrimitive(target, context) && ConvertsImplicitly(source, target, context);
    }

    // Between two concepts the generated target concept declares the implicit conversion (SemanticProjectionConversions).
    static bool ConvertsImplicitly(SemanticTypeReference source, SemanticTypeReference target, SemanticApplicationContext context) =>
        (source.Kind == SemanticTypeReferenceKind.Concept && target.Kind == SemanticTypeReferenceKind.Primitive && !target.IsOptional) ||
        (source.Kind == SemanticTypeReferenceKind.Primitive && target.Kind == SemanticTypeReferenceKind.Concept) ||
        (source.Kind == SemanticTypeReferenceKind.Concept && target.Kind == SemanticTypeReferenceKind.Concept && BothUnconstrained(source, target, context));

    static bool BothUnconstrained(SemanticTypeReference source, SemanticTypeReference target, SemanticApplicationContext context) =>
        context.Concepts[source.Target].Values.IsEmpty && context.Concepts[target.Target].Values.IsEmpty;

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
        SourceMatchesKey(mapping, key);

    static bool SourceMatchesKey(SemanticProjectionMapping mapping, SemanticProjectionKey key) => key switch
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
        var paths = mappings.Where(mapping => mapping.Operation is SemanticProjectionOperation.Set or SemanticProjectionOperation.SetOnce or SemanticProjectionOperation.Add or
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
}
