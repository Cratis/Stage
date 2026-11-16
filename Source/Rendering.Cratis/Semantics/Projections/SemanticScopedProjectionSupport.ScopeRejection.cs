// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

internal static partial class SemanticScopedProjectionSupport
{
    sealed class ScopeRejection(
        SemanticProjectionScope scope,
        SemanticApplicationContext context,
        IReadOnlyList<SemanticProperty> properties,
        bool child,
        SemanticId? identity,
        bool isNested)
    {
        SemanticId? _protectedIdentity;

        /// <summary>Gets a value indicating whether an ancestor of this scope is a child collection.</summary>
        public bool UnderChildren { get; init; }

        bool IsRoot => !child && !isNested;

        bool IsCompositeRoot => IsRoot && properties.Count(property => property.IsIdentifier) > 1;

        bool IsUnverifiedEveryJoinLevel => !IsRoot && scope.Joins.Length > 0;

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

        static bool SharesTargetPrefix(SemanticProjectionMapping mapping, SemanticProjectionMapping literal)
        {
            var length = Math.Min(mapping.Target.Length, literal.Target.Length);
            return mapping.Target.Take(length).SequenceEqual(literal.Target.Take(length));
        }

        static bool IsComposite(SemanticProjectionKey? key) => key is SemanticProjectionCompositeKey;

        static bool IsChildrenOrJoinRemovalInNested(SemanticProjectionNested nested) =>
            nested.Scope.Children.Length > 0 || nested.Scope.JoinRemovals.Length > 0;

        // Chronicle keys a nested join by the nested object's identifier or a property named id and backfills it; the reference does not.
        static bool IsNestedKey(SemanticProperty property) =>
            property.IsIdentifier || property.Name.Equals("id", StringComparison.OrdinalIgnoreCase);

        static bool IsEventPropertyTotal(SemanticProjectionMapping mapping) =>
            mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract &&
            mapping.Factor is null && mapping.Source is SemanticProjectionEventProperty;

        static bool IsCollectionOfComposites(SemanticProperty? property) =>
            property?.Type.IsCollection == true && property.Type.Kind == SemanticTypeReferenceKind.CompositeType;

        string? LimitRejection() => ChronicleLimitRejection() ?? EveryLimitRejection() ?? JoinRemovalRejection() ?? NestedJoinKeyRejection();

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

            if (scope.Nested.Any(IsChildrenOrJoinRemovalInNested))
            {
                return "Children blocks and remove via join inside nested are unsupported: Chronicle's engine does not wire them.";
            }

            if (IsJoinInNestedUnderChildren())
            {
                return "A join inside a nested object under children is not wired by Chronicle's engine.";
            }

            if (IsJoinInNestedBelowRoot())
            {
                return "A join inside a nested object is admitted only when the nested object is directly at the read-model root; a nested object inside another nested object or inside a children element cannot carry a join.";
            }

            return null;
        }

        bool IsJoinInNestedUnderChildren() =>
            (child || UnderChildren) && scope.Nested.Any(nested => nested.Scope.Joins.Length > 0);

        bool IsJoinInNestedBelowRoot() =>
            !IsRoot && scope.Nested.Any(nested => nested.Scope.Joins.Length > 0);

        bool IsJoinOnNestedKey(SemanticProjectionJoin join) =>
            properties.SingleOrDefault(property => property.Id == join.On) is { } on && IsNestedKey(on);

        string? NestedJoinKeyRejection() =>
            isNested && scope.Joins.Any(IsJoinOnNestedKey)
                ? "A nested join on the nested object's key cannot render: Chronicle backfills by the nested key, the Screenplay reference does not."
                : null;

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

            if (PropertyUnsetUnderAll() is { } unset)
            {
                return $"All cannot render while '{unset.Name}' is neither mapped by all nor optional: the reference leaves it unset (null) on instances for unrelated events, but Chronicle materializes its default value.";
            }

            if (scope.Every is { IncludeChildren: true } && HasChildScopes())
            {
                return "Every with IncludeChildren is not yet verified by Stage for child and nested scopes.";
            }

            return null;
        }

        SemanticProperty? PropertyUnsetUnderAll() =>
            scope.Every is { SubscribesToAllEvents: true } unrelated ? properties.FirstOrDefault(property => IsUnsetUnderAll(property, unrelated)) : null;

        bool IsUnsetUnderAll(SemanticProperty property, SemanticProjectionEvery every) =>
            !property.IsIdentifier && !every.Mappings.Any(mapping => mapping.Target.Contains(property.Id)) && !NullWhenUnset(property);

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
        string? TotalTypeRejection() =>
            scope.From.Select(FromTotalTypeRejection).FirstOrDefault(reason => reason is not null);

        string? FromTotalTypeRejection(SemanticProjectionFrom from) =>
            context.Events.TryGetValue(from.EventContract, out var @event)
                ? from.Mappings.Where(IsEventPropertyTotal).Select(mapping => TotalTypeMismatch(mapping, @event)).FirstOrDefault(reason => reason is not null)
                : null;

        string? TotalTypeMismatch(SemanticProjectionMapping mapping, SemanticEventContract @event)
        {
            var source = EventProperty(((SemanticProjectionEventProperty)mapping.Source!).Path, @event.Properties, context);
            var target = Target(mapping.Target, properties, context);
            return source is not null && target is not null && !SameTotalType(source.Type, target.Type, context)
                ? $"A running total '{target.Name}' adds the event value '{source.Name}', which has another type: Chronicle adds an event property only where it has the type the total holds, so give both the same type."
                : null;
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
                mapping.Source is not SemanticProjectionEventSourceIdentity and not SemanticProjectionLiteral and not SemanticProjectionEventContextValue) ||
            !EveryMappingsSupported(every.Mappings, properties, context, _protectedIdentity);

        bool CollidesWithFromMapping(SemanticProjectionMapping literal) =>
            scope.From.SelectMany(from => from.Mappings).Any(mapping =>
                SharesTargetPrefix(mapping, literal) &&
                (mapping.Target.Length != literal.Target.Length || mapping.Operation != SemanticProjectionOperation.Set));

        string? CompositeKeyRejection()
        {
            if (HasCompositeKey())
            {
                return "Composite-key read models need a keyed lookup by composite key that Stage cannot generate yet; Stage#839 and Chronicle#4265 remain open, so Stage cannot look up those instances by their composite key.";
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
            for (var index = 0; index < scope.From.Length; index++)
            {
                if (FromBlockRejection(scope.From[index]) is { } reason)
                {
                    return $"{FromDescription(scope.From[index], index)} cannot render: {reason}";
                }
            }

            // A composite root key establishes every key part itself, so the parts are not left for the mappings.
            var required = IsCompositeRoot ? [.. properties.Where(property => !property.IsIdentifier)] : properties;
            var everyLiterals = NativeEveryMappings(scope);
            var joined = child ? scope.Joins.SelectMany(join => join.Mappings) : [];
            if (scope.From.Length == 0 || scope.From.Any(from => EstablishesRequiredProperties(from.Mappings.Concat(everyLiterals).Concat(joined), required, context, _protectedIdentity)))
            {
                return null;
            }

            var missing = scope.From.Select((from, index) =>
                $"{FromDescription(from, index)} is missing required mappings for {string.Join(", ", required.Where(property =>
                    !EstablishesRequiredProperties(from.Mappings.Concat(everyLiterals).Concat(joined), [property], context, _protectedIdentity)).Select(property => $"'{property.Name}'"))}");
            return $"At least one from block must establish every required read-model property; {string.Join("; ", missing)}.";
        }

        string FromDescription(SemanticProjectionFrom from, int index) =>
            $"From block #{index + 1} for event '{(context.Events.TryGetValue(from.EventContract, out var @event) ? @event.Name : from.EventContract.ToString())}'";

        string? FromBlockRejection(SemanticProjectionFrom from)
        {
            if (!context.Events.TryGetValue(from.EventContract, out var @event))
            {
                return "the event contract cannot be resolved.";
            }

            if (!KeySupported(from.Key, @event, context) && !(IsRoot && ConstantKeySupported(from.Key)) && !PartsKeySupported(from.Key, @event))
            {
                return $"key '{KeyDescription(from.Key, @event)}' cannot render: {KeyReason(from.Key)}.";
            }

            if (from.ParentKey is { } parent)
            {
                if (!child)
                {
                    return $"parent key '{KeyDescription(parent, @event)}' cannot render: parent keys are only supported inside children.";
                }

                if (!KeySupported(parent, @event, context))
                {
                    return $"parent key '{KeyDescription(parent, @event)}' cannot render: {KeyReason(parent)}.";
                }
            }

            var targets = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < from.Mappings.Length; index++)
            {
                var mapping = from.Mappings[index];
                var description = $"mapping #{index + 1} '{PropertyPath(mapping.Target, properties)}' ({mapping.Operation})";
                if (!targets.Add(string.Join('.', mapping.Target)))
                {
                    return $"{description} cannot render: the target is mapped more than once in this from block.";
                }

                if (!MappingSupported(mapping, properties, @event, context, _protectedIdentity, from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }, fromBlock: true))
                {
                    return $"{description} cannot render: {MappingReason(mapping, @event)}.";
                }
            }

            return null;
        }

        string KeyDescription(SemanticProjectionKey key, SemanticEventContract @event) => key switch
        {
            SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } => PropertyPath(property.Path, @event.Properties),
            SemanticProjectionValueKey { Value: SemanticProjectionEventContextValue value } => $"$context.{value.Path}",
            SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } => "$eventSourceId",
            SemanticProjectionValueKey { Value: SemanticProjectionLiteral literal } => $"literal {literal.Value}",
            _ => key.Kind.ToString()
        };

        string KeyReason(SemanticProjectionKey key) => key switch
        {
            SemanticProjectionValueKey { Value: SemanticProjectionEventContextValue } => "event-context keys have no admitted Chronicle lowering",
            SemanticProjectionValueKey { Value: SemanticProjectionEventProperty } => "the event-property path cannot be resolved",
            SemanticProjectionValueKey { Value: SemanticProjectionLiteral } when IsRoot => "a root constant key requires non-empty text safe in Chronicle's unquoted value expression",
            _ => "only the event source identity or a resolvable event-property path is supported"
        };

        string PropertyPath(System.Collections.Immutable.ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> root)
        {
            var names = new List<string>();
            foreach (var id in path)
            {
                var property = root.SingleOrDefault(candidate => candidate.Id == id);
                names.Add(NameOf(property, id));
                root = property is null ? [] : ChildProperties(property, context);
            }

            return string.Join('.', names);
        }

        string NameOf(SemanticProperty? property, SemanticId id) => property?.Name ?? id.ToString();

        string MappingReason(SemanticProjectionMapping mapping, SemanticEventContract @event)
        {
            var target = Target(mapping.Target, properties, context);
            if (target is null)
            {
                return "the target property path cannot be resolved";
            }

            if (HasOptionalIntermediate(mapping.Target, properties, context))
            {
                return "the target path traverses an optional composite";
            }

            if (target.Type.IsCollection)
            {
                return "a collection requires Set from one direct event property of exactly the same non-optional collection type";
            }

            return mapping.Operation switch
            {
                SemanticProjectionOperation.Clear => "Clear requires an optional target and no source value",
                SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => $"{mapping.Operation} requires no source value",
                SemanticProjectionOperation.Set or SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract => SourceReason(mapping, @event),
                _ => "the mapping operation has no admitted Chronicle equivalent"
            };
        }

        string SourceReason(SemanticProjectionMapping mapping, SemanticEventContract @event) => mapping.Source switch
        {
            SemanticProjectionEventContextValue value => $"source '$context.{value.Path}' has no admitted Chronicle mapping",
            SemanticProjectionEventProperty property => $"the source event-property path cannot be resolved: '{PropertyPath(property.Path, @event.Properties)}'",
            null => $"{mapping.Operation} requires a source value",
            _ when mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract => $"{mapping.Operation} requires an event-property source or a supported numeric zero identity",
            SemanticProjectionLiteral => "the text literal is unsafe in Chronicle's unquoted value expression",
            SemanticProjectionEventSourceIdentity => "the event source identity is incompatible with the target identity/type and the event's produced destination type",
            _ => $"source kind '{mapping.Source.Kind}' has no admitted Chronicle mapping"
        };

        bool ParentKeyUnsupported(SemanticProjectionKey? parentKey, SemanticEventContract @event) =>
            child ? parentKey is not null && !KeySupported(parentKey, @event, context) : parentKey is not null;

        string? JoinRejection() =>
            scope.Joins.Any(join => !JoinSupported(join)) ? "A join has an unsupported correlation key or mapping." : null;

        bool JoinSupported(SemanticProjectionJoin join) =>
            context.Events.TryGetValue(join.EventContract, out var @event) &&
            properties.SingleOrDefault(property => property.Id == join.On) is { } on &&
            (IsRoot ? RootJoinKeySupported(join.Key, on, @event, context) : join.Key is null) &&
            (!child || join.On == identity) && MappingsSupported(join.Mappings, properties, @event, context, _protectedIdentity, false);

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

            return new ScopeRejection(nested.Scope, context, composite.Properties, false, null, true) { UnderChildren = child || UnderChildren }.Find() is { } reason
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

            return new ScopeRejection(children.Scope, context, element.Properties, true, children.IdentifiedBy, false) { UnderChildren = true }.Find() is { } reason
                ? $"A children block cannot render: {reason}"
                : null;
        }
    }
}
