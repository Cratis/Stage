// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

internal static partial class SemanticRunProjectionAdmission
{
    [GeneratedRegex(@"\A[\w ._/:*+-]*\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex FluentTextLiteral { get; }

    internal static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> reachableEvents) =>
        RequiredLookupRefusal(plan, specification) ?? CheckProjections(plan, specification, reachableEvents);

    internal static SemanticUnsupportedCapability? CheckFact(SemanticExecutionPlan plan, SemanticSpecificationEvent fact)
    {
        // Reuse the same concrete source/key checks as fixture admission before a reaction fact is appended.
        var fixture = new SemanticSpecification(fact.EventContract, "Reaction production", [fact], [], null, [], [], [], []);

        return Check(plan, fixture, [fact.EventContract]);
    }

    // An empty-world assertion must not turn an unadmitted required lookup into a pass.
    static SemanticUnsupportedCapability? RequiredLookupRefusal(SemanticExecutionPlan plan, SemanticSpecification specification) =>
        specification.ThenQueries.Select(assertion => plan.Queries.GetValueOrDefault(assertion.Query))
            .Where(query => query is { Cardinality: SemanticQueryCardinality.One })
            .Select(query => SemanticPlanAdmission.Check(plan, query!.Id))
            .FirstOrDefault(refusal => refusal is not null);

    static SemanticUnsupportedCapability? CheckProjections(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> reachableEvents)
    {
        var emptyQuery = specification.When is null && specification.WhenAppended is null && specification.GivenEvents.IsEmpty &&
            specification.GivenReadModels.IsEmpty && specification.ThenQueries.Length == 1 && specification.ThenQueries[0].Results.IsEmpty;

        var reachable = reachableEvents.ToHashSet();

        // Match the reference projector's identity order, not the plan dictionary's hash order.
        var requested = plan.Projections.Values.Where(value => value.Target != SemanticProjectionTargetKind.Event && (
            specification.GivenReadModels.Any(state => state.ReadModel == value.ReadModel) ||
            specification.ThenReadModels.Any(state => state.ReadModel == value.ReadModel) ||
            specification.ThenAbsentReadModels.Any(state => state.ReadModel == value.ReadModel) ||
            specification.ThenQueries.Any(query => plan.Queries.TryGetValue(query.Query, out var target) && target.ReadModel == value.ReadModel) ||
            reachable.Any(eventContract => SemanticProjectionObservation.Observes(value, eventContract))))
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal).ToArray();
        var duplicate = requested.Select(projection => projection.ReadModel).FirstOrDefault(id => plan.Projections.Values.Count(projection => projection.Target != SemanticProjectionTargetKind.Event && projection.ReadModel == id) > 1);
        if (duplicate.IsSet)
        {
            return new(StageExecutionCapability.Projection, duplicate.ToString(), "A per-run read model needs exactly one projection.");
        }
        foreach (var projection in requested)
        {
            var reason = Rejection(plan, projection, plan.ReadModels[projection.ReadModel], specification, reachable);
            if (reason is not null)
            {
                return new(StageExecutionCapability.Projection, projection.Id.ToString(), reason);
            }
        }

        foreach (var seed in specification.GivenReadModels.GroupBy(seed => (seed.ReadModel, seed.Key)).Select(group => group.Last()))
        {
            if (!plan.ReadModels.TryGetValue(seed.ReadModel, out var model))
            {
                return new(StageExecutionCapability.GivenReadModel, seed.ReadModel.ToString(), "The seeded read model is not in the plan.");
            }
            if (plan.Projections.Values.Any(projection => projection.ReadModel == seed.ReadModel) && SeedStructuredIncomplete(plan, model, seed.Values))
            {
                return new(StageExecutionCapability.GivenReadModel, seed.ReadModel.ToString(), IncompleteStructuredValue);
            }
            if (plan.Projections.Values.Any(projection => projection.ReadModel == seed.ReadModel) &&
                (model.Properties.Any(property => seed.Values.All(value => value.TargetProperty != property.Id || value.Value is SemanticNullValue)) ||
                    !seed.Values.Any(value => value.TargetProperty == model.Properties.Single(property => property.IsIdentifier).Id && SameScalar(value.Value, seed.Key))))
            {
                return new(StageExecutionCapability.GivenReadModel, seed.ReadModel.ToString(), "A projected read-model seed requires every scalar property and an identifier equal to its key; partial state cannot preserve property presence through the concrete scenario.");
            }
        }

        var assertedModels = specification.ThenReadModels.Select(assertion => assertion.ReadModel)
            .Concat(specification.ThenAbsentReadModels.Select(assertion => assertion.ReadModel));
        foreach (var readModel in assertedModels)
        {
            if (!plan.ReadModels.ContainsKey(readModel) ||
                (plan.Projections.Values.All(projection => projection.ReadModel != readModel) && specification.GivenReadModels.All(seed => seed.ReadModel != readModel)))
            {
                return new(StageExecutionCapability.Projection, readModel.ToString(), "The asserted read model has no projection in this run.");
            }
        }
        foreach (var assertion in specification.ThenQueries)
        {
            if (emptyQuery && plan.Queries.ContainsKey(assertion.Query)) continue;
            if (!plan.Queries.TryGetValue(assertion.Query, out var query) ||
                !plan.ReadModels.TryGetValue(query.ReadModel, out var model) ||
                (!query.KeyParts.IsDefaultOrEmpty && query.KeyParts.Length > 1) ||
                (query.Filters.IsEmpty && (query.Delivery != SemanticQueryDelivery.Snapshot || query.Cardinality != SemanticQueryCardinality.ZeroOrOne ||
                    model.Properties.Where(property => property.IsIdentifier).Select(property => (SemanticId?)property.Id).ToArray() is not [var identifier] || identifier != query.KeyProperty)) ||
                (plan.Projections.Values.All(projection => projection.ReadModel != query.ReadModel) && specification.GivenReadModels.All(seed => seed.ReadModel != query.ReadModel)))
            {
                return new(StageExecutionCapability.Query, assertion.Query.ToString(), "Only optional snapshot queries by read-model identifier are admitted.");
            }
        }
        return null;
    }

    static string? ShapeRejection(SemanticExecutionPlan plan, SemanticReadModel readModel, SemanticProjectionScope? scope)
    {
        var identifiers = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        if (identifiers.Length > 1)
        {
            return $"Read model '{readModel.Name}' has several key parts (ESM v27 composite read-model keys); the per-run projection scenario seeds and looks up instances by one event-source key, so it cannot run composite read-model keys yet.";
        }
        if (identifiers.Length != 1 || identifiers[0].Type.IsOptional)
        {
            return "Per-run projections need one required identifier.";
        }
        return !IsScalar(identifiers[0].Type) || identifiers[0].Type.IsCollection ||
            readModel.Properties.Any(property => (property.Type.IsCollection || !IsScalar(property.Type)) && scope?.Children.Any(child => child.Property == property.Id) != true && !WholeValueCopy(plan, property, scope))
            ? "Per-run projections need scalar read model properties without collections or composite types."
            : null;
    }

    static bool IsScalar(SemanticTypeReference type) =>
        type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept;

    static string? Rejection(SemanticExecutionPlan plan, SemanticProjection projection, SemanticReadModel readModel, SemanticSpecification specification, HashSet<SemanticId> reachable)
    {
        if (ProjectionReferencedEventNamesAreUnique.Contracts(projection)
            .Any(id => plan.Events.TryGetValue(id, out var @event) && !SemanticRunAdmission.RuntimePropertyNamesAreUnique(@event.Properties)))
        {
            return "A projection references an event whose property names are not unique under case-insensitive runtime JSON binding.";
        }
        if (ProjectionReferencedEventNamesAreUnique.Contracts(projection)
            .Any(id => plan.Events.TryGetValue(id, out var @event) &&
                @event.Properties.Any(property => !ChronicleEventPropertyNameIsSafe.Check(property.Name))))
        {
            return "An event property name collides with Chronicle expression syntax.";
        }
        if (ShapeRejection(plan, readModel, projection.Scope) is { } shape)
        {
            return shape;
        }
        if (readModel.Properties.Any(ChronicleReadModelPropertyNamesAreSafe.CollidesWithRootDocumentKey))
        {
            return "A non-identifier read-model property named Id collides with Chronicle's document key.";
        }
        if (!ChronicleReadModelPropertyNamesAreSafe.Check(readModel.Properties))
        {
            return "A read-model property name collides with Chronicle's property paths or another generated property.";
        }
        if (!SemanticRunAdmission.RuntimePropertyNamesAreUnique(readModel.Properties))
        {
            return "Read-model property names are not unique under case-insensitive runtime JSON binding.";
        }
        if (readModel.Properties.FirstOrDefault(property => Primitive(plan, property.Type) == SemanticPrimitiveType.DateTime) is { } dateTime)
        {
            return $"DateTime read-model property '{readModel.Name}.{dateTime.Name}' cannot preserve ordinal round-trip text: DateTimeOffset materialization renders authored 'Z' as '+00:00'.";
        }
        var identifier = readModel.Properties.Single(property => property.IsIdentifier);
        var primitive = identifier.Type.Kind == SemanticTypeReferenceKind.Concept
            ? plan.Model.Application.Concepts.Single(concept => concept.Id == identifier.Type.Target).Primitive : identifier.Type.Primitive;
        if (primitive is not (SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text))
        {
            return "A per-run projection's identifier must be a text or UUID key.";
        }

        if (projection.Scope is { } scope)
        {
            if (scope.Children.Any(child => ChildNullMappings(plan, child.Scope)))
                return "An optional child source or null literal cannot preserve the distinction between a missing member and an explicit null in Chronicle's typed child instances.";
            if ((!scope.Children.IsEmpty || !scope.Joins.IsEmpty) && specification.GivenReadModels.Any(seed => seed.ReadModel == readModel.Id))
                return "Structured projection fixtures with children or joins are not admitted: replacing parent history must also replace child and join history.";
            var rejection = SemanticScopedProjectionSupport.Rejection(scope, new SemanticApplicationContext(plan), readModel.Properties);
            if (rejection is not null)
            {
                return rejection;
            }
            if (HasProduct(scope, reachable))
            {
                return "A projection adds or subtracts a product of two event values (ESM v29); Chronicle's projection engine adds one event property, so Stage does not run it.";
            }
            if (readModel.Properties.All(property => property.IsIdentifier || property.Type.IsOptional) && scope.From.Any(from => from.Mappings.IsEmpty))
            {
                return "A mapping-free from block on a read model without required properties does not materialize a Chronicle read model.";
            }
            if (scope.Removals.Any(removal => !RemovalKeySupported(plan, removal, identifier.Type)))
            {
                return "A per-run removal key must be the event source or one required scalar event property with the identifier's value type.";
            }
            var sourceKeyedRemovals = scope.Removals.Where(removal => removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity });
            if (scope.Every is { SubscribesToAllEvents: true }
                ? reachable.Any(eventContract => !scope.Removals.Any(removal => removal.EventContract == eventContract &&
                    removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty }) &&
                    !ScopedKeysMatch(plan, specification, eventContract, identifier.Type))
                : scope.From.Where(from => from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity })
                    .Select(from => from.EventContract).Concat(sourceKeyedRemovals.Select(removal => removal.EventContract))
                    .Any(eventContract => reachable.Contains(eventContract) && !ScopedKeysMatch(plan, specification, eventContract, identifier.Type)))
            {
                return "A scoped event-source key must have the read-model identifier's value type and enumeration values.";
            }
            if (projection.Transitions.Length > 0 ||
                scope.Removals.Any(removal => removal.ParentKey is not null) ||
                scope.JoinRemovals.Length > 0 || scope.Nested.Length > 0 ||
                scope.Joins.Any(join => join.Mappings.Any(mapping => !MappingSupported(plan, join.EventContract, mapping))) ||
                scope.Children.Any(child => !ChildrenSupported(plan, specification, child, readModel.Properties, identifier.Type)) ||
                (scope.Every is { } every && every.Mappings.Any(mapping => mapping.Operation != SemanticProjectionOperation.Set ||
                    mapping.Target.Length != 1 || mapping.Source is not (SemanticProjectionEventSourceIdentity or SemanticProjectionLiteral))) ||
                scope.From.Any(from => !UnobservedProduct(from, reachable) && (from.ParentKey is not null || !FromKeySupported(plan, specification, from, identifier.Type) ||
                    from.Mappings.Any(mapping => !MappingSupported(plan, from.EventContract, mapping)))))
            {
                return "This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.";
            }
            return StructuredValueRefusal(plan, scope, specification);
        }

        if (readModel.Properties.Any(property => property.Type.IsCollection || !IsScalar(property.Type)) || projection.Transitions.Length != 1 || !FlatMatches(plan, specification, projection.Transitions[0], readModel))
        {
            return "A flat transition must use scalar event mappings and a key equal to the fact's event source.";
        }
        return null;
    }

    // A product in a from block whose event the run never reaches cannot fire, so it is skipped rather than refused.
    static bool HasProduct(SemanticProjectionScope scope, HashSet<SemanticId> reachable) =>
        scope.From.Any(from => reachable.Contains(from.EventContract) && from.Mappings.Any(mapping => mapping.Factor is not null)) ||
        scope.Joins.Any(join => join.Mappings.Any(mapping => mapping.Factor is not null)) ||
        scope.Children.Any(child => HasProduct(child.Scope, reachable));

    static bool UnobservedProduct(SemanticProjectionFrom from, HashSet<SemanticId> reachable) =>
        !reachable.Contains(from.EventContract) && from.Mappings.Any(mapping => mapping.Factor is not null);

    static bool ChildNullMappings(SemanticExecutionPlan plan, SemanticProjectionScope scope)
    {
        bool HasNull(SemanticId eventContract, System.Collections.Immutable.ImmutableArray<SemanticProjectionMapping> mappings) => mappings.Any(mapping => mapping.Source switch
        {
            SemanticProjectionLiteral { Value: SemanticNullValue } => true,
            SemanticProjectionEventProperty { Path: [var id] } => plan.Events[eventContract].Properties.Single(property => property.Id == id).Type.IsOptional,
            _ => false
        });
        return scope.From.Any(from => HasNull(from.EventContract, from.Mappings)) || scope.Joins.Any(join => HasNull(join.EventContract, join.Mappings)) || scope.Children.Any(child => ChildNullMappings(plan, child.Scope));
    }

    static bool ChildrenSupported(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionChildren child, System.Collections.Immutable.ImmutableArray<SemanticProperty> parentProperties, SemanticTypeReference parentIdentity)
    {
        var collection = parentProperties.Single(property => property.Id == child.Property);
        var properties = plan.Model.Application.Types.Single(type => type.Id == collection.Type.Target).Properties;
        var identity = properties.Single(property => property.Id == child.IdentifiedBy).Type;
        var scope = child.Scope;
        bool ParentMatches(SemanticProjectionKey? key, SemanticId eventContract) => key switch
        {
            null or SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } => ScopedKeysMatch(plan, specification, eventContract, parentIdentity),
            SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } } =>
                plan.Events[eventContract].Properties.Single(property => property.Id == id) is { Type: { IsOptional: false, IsCollection: false } } source &&
                IsScalar(source.Type) && SourceTypeMatches(plan, source.Type, parentIdentity) && ScopedKeysMatch(plan, specification, eventContract, parentIdentity, id),
            _ => false
        };
        return IsScalar(identity) && !identity.IsOptional && !identity.IsCollection &&
            Primitive(plan, identity) is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid &&
            ChronicleReadModelPropertyNamesAreSafe.Check(properties, rootLevel: false) && SemanticRunAdmission.RuntimePropertyNamesAreUnique(properties) &&
            properties.All(property => (IsScalar(property.Type) && !property.Type.IsCollection && Primitive(plan, property.Type) != SemanticPrimitiveType.DateTime) || scope.Children.Any(child => child.Property == property.Id)) &&
            scope.Nested.IsEmpty && scope.Children.IsEmpty && scope.JoinRemovals.IsEmpty && scope.Every is null &&
            scope.From.All(from => FromKeySupported(plan, specification, from, identity) && ParentMatches(from.ParentKey, from.EventContract) && from.Mappings.All(MappingSupported)) &&
            scope.Removals.All(removal => RemovalKeySupported(plan, removal, identity) && ParentMatches(removal.ParentKey, removal.EventContract)) &&
            scope.Joins.All(join => join.Mappings.All(MappingSupported)) &&
            scope.Children.All(nested => ChildrenSupported(plan, specification, nested, properties, identity));
    }

    static bool MappingSupported(SemanticProjectionMapping mapping) => MappingSupported(null, null, mapping);

    // Only root from and join blocks pass the plan and event, which admits a Set from a nested scalar event path.
    static bool MappingSupported(SemanticExecutionPlan? plan, SemanticId? eventContract, SemanticProjectionMapping mapping)
    {
        if (mapping.Target.Length != 1 || mapping.Factor is not null) return false;
        return mapping.Operation switch
        {
            SemanticProjectionOperation.Set => mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 } or SemanticProjectionEventSourceIdentity or SemanticProjectionLiteral ||
                (plan is not null && eventContract is { } contract && mapping.Source is SemanticProjectionEventProperty { Path.Length: > 1 } nested &&
                    NestedPathSupported(plan, plan.Events[contract], nested)),
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract => mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 } or SemanticProjectionLiteral,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            _ => false
        };
    }

    static bool FromKeySupported(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionFrom from, SemanticTypeReference identifier)
    {
        if (from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) return true;
        if (from.Key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue text } })
        {
            return Primitive(plan, identifier) == SemanticPrimitiveType.Text && text.Value.Length > 0 &&
                FluentTextLiteral.IsMatch(text.Value) &&
                (identifier.Kind != SemanticTypeReferenceKind.Concept ||
                    plan.Model.Application.Concepts.Single(concept => concept.Id == identifier.Target).Values is not { IsEmpty: false } values || values.Contains(text.Value));
        }
        return from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } } &&
            plan.Events[from.EventContract].Properties.SingleOrDefault(property => property.Id == id) is { Type: { IsOptional: false, IsCollection: false } } source &&
            IsScalar(source.Type) && SourceTypeMatches(plan, source.Type, identifier) &&
            ScopedKeysMatch(plan, specification, from.EventContract, identifier, id);
    }

    static bool RemovalKeySupported(SemanticExecutionPlan plan, SemanticProjectionRemoval removal, SemanticTypeReference identifier)
    {
        if (removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) return true;
        return removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } } &&
            plan.Events.TryGetValue(removal.EventContract, out var @event) &&
            @event.Properties.SingleOrDefault(candidate => candidate.Id == id) is { Type: { IsOptional: false, IsCollection: false } } property &&
            IsScalar(property.Type) && SourceTypeMatches(plan, property.Type, identifier);
    }

    static bool SourceTypeMatches(SemanticExecutionPlan plan, SemanticTypeReference source, SemanticTypeReference identifier) =>
        Primitive(plan, source) == Primitive(plan, identifier);

    static bool FlatMatches(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionTransition transition, SemanticReadModel readModel)
    {
        if (!plan.Events.TryGetValue(transition.EventContract, out var @event) ||
            transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
            transition.AffectedInstance.Key is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } key)
        {
            return false;
        }
        return @event.Properties.Any(property => property.Id == key.Target) &&
            SemanticFlatProjectionSupport.MappingsMatch(transition.Mappings, readModel.Properties, @event.Properties, SemanticExpressionRootKind.Event) &&
            transition.Mappings.All(mapping => mapping.TargetProperty != readModel.Properties.Single(property => property.IsIdentifier).Id ||
                (mapping.Source is SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } source && source.Target == key.Target)) &&
            KeyMatchesSource(plan, specification, transition, key.Target);
    }

    static SemanticPrimitiveType Primitive(SemanticExecutionPlan plan, SemanticTypeReference type) =>
        type.Kind == SemanticTypeReferenceKind.Concept
            ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive;

    static bool ScopedKeysMatch(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticId eventContract, SemanticTypeReference identifier, SemanticId? keyProperty = null)
    {
        bool Matches(SemanticEventSourceIdentity? source) => source is not null &&
            SourceTypeMatches(plan, source.Type, identifier) &&
            source.Value is SemanticTextValue text &&
            (Primitive(plan, identifier) != SemanticPrimitiveType.Uuid ||
                (Guid.TryParseExact(text.Value, "D", out var uuid) && uuid.ToString("D") == text.Value)) &&
            (identifier.Kind != SemanticTypeReferenceKind.Concept ||
                plan.Model.Application.Concepts.Single(concept => concept.Id == identifier.Target).Values is not { IsEmpty: false } values || values.Contains(text.Value));

        SemanticEventSourceIdentity? Key(SemanticEventSourceIdentity? source, IEnumerable<SemanticPropertyValue> values) =>
            keyProperty is { } property ? new(
                plan.Events[eventContract].Properties.Single(candidate => candidate.Id == property).Type,
                values.SingleOrDefault(value => value.TargetProperty == property)?.Value ?? SemanticValue.Null) : source;

        if (specification.GivenEvents.Any(given => given.EventContract == eventContract && !Matches(Key(given.EventSource, given.Values))) ||
            (specification.WhenAppended is { } appended && appended.EventContract == eventContract && !Matches(Key(appended.EventSource, appended.Values))))
        {
            return false;
        }
        if (specification.When is not { } when || !plan.Commands.TryGetValue(when.Command, out var command) ||
            !specification.ThenErrors.IsEmpty || specification.ThenDenied)
        {
            return true;
        }
        foreach (var produced in command.Produces.Where(produced => produced.EventContract == eventContract))
        {
            var destination = keyProperty is { } key
                ? produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == key)?.Source
                : produced.Destination ?? command.Destination?.Value;
            if (keyProperty is not null && destination is null) return false;
            var value = destination switch
            {
                SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } property =>
                    when.Values.Concat(when.GeneratedValues).SingleOrDefault(input => input.TargetProperty == property.Target)?.Value,
                SemanticValueExpression literal => literal.Value,
                null => when.EventSource?.Value,
                _ => null
            };
            var type = destination is SemanticResolvedExpression resolved
                ? command.Properties.Single(property => property.Id == resolved.Target).Type : command.Destination?.Type ?? when.EventSource?.Type;
            if (keyProperty is { } propertyKey) type = plan.Events[eventContract].Properties.Single(property => property.Id == propertyKey).Type;
            if (type is null || value is null || !Matches(new(type, value)))
            {
                return false;
            }
        }
        return true;
    }

    static bool KeyMatchesSource(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionTransition transition, SemanticId key)
    {
        foreach (var given in specification.GivenEvents.Where(given => given.EventContract == transition.EventContract))
        {
            var value = given.Values.SingleOrDefault(value => value.TargetProperty == key)?.Value;
            if (value is null || given.EventSource is null || !SameScalar(value, given.EventSource.Value))
            {
                return false;
            }
        }
        if (specification.WhenAppended?.EventContract == transition.EventContract)
        {
            var appended = specification.WhenAppended;
            var value = appended.Values.SingleOrDefault(value => value.TargetProperty == key)?.Value;
            if (value is null || appended.EventSource is null || !SameScalar(value, appended.EventSource.Value))
            {
                return false;
            }
        }
        if (specification.When is { } when && plan.Commands.TryGetValue(when.Command, out var command))
        {
            foreach (var produced in command.Produces.Where(produced => produced.EventContract == transition.EventContract && specification.ThenErrors.IsEmpty && !specification.ThenDenied))
            {
                var mapping = produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == key);
                var destination = produced.Destination ?? command.Destination?.Value;
                if (mapping?.Source is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } source ||
                    destination is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } target || source.Target != target.Target)
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool SameScalar(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticTextValue a, SemanticTextValue b) => a.Value == b.Value,
        (SemanticNumberValue a, SemanticNumberValue b) => a.Value == b.Value,
        (SemanticBooleanValue a, SemanticBooleanValue b) => a.Value == b.Value,
        _ => false
    };
}
