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
    const string JoinIdentityRefusal = "A join's event-source identity must have the joined key's value type and enumeration values.";

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
            if (AuthorsExplicitNestedNull(specification))
            {
                return new(
                    StageExecutionCapability.Projection,
                    projection.Id.ToString(),
                    "A composite or collection value authors an explicit null member; the typed per-run models cannot tell it from an absent member.");
            }

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

    static bool AuthorsExplicitNestedNull(SemanticSpecification specification) =>
        specification.GivenEvents.SelectMany(given => given.Values)
            .Concat(specification.WhenAppended?.Values ?? [])
            .Concat(specification.When?.Values ?? [])
            .Concat(specification.GivenReadModels.SelectMany(seed => seed.Values))
            .Any(value => HasNestedNull(value.Value));

    static bool HasNestedNull(SemanticValue value) => value switch
    {
        SemanticArrayValue array => array.Values.Any(element => element is SemanticNullValue || HasNestedNull(element)),
        SemanticCompositeValue composite => composite.Properties.Any(property => property.Value is SemanticNullValue || HasNestedNull(property.Value)),
        _ => false
    };

    static string? ShapeRejection(SemanticReadModel readModel, SemanticProjectionScope? scope)
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
        return !IsScalar(identifiers[0].Type) || identifiers[0].Type.IsCollection
            ? "Per-run projections need a scalar, non-collection identifier."
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
        if (ShapeRejection(readModel, projection.Scope) is { } shape)
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
            if (HasProductOutsideFrom(scope))
            {
                return "A join or child level adds or subtracts a product of two event values (ESM v29); only a root from block can carry the product on its event.";
            }
            if (ProductRejection(plan, scope, readModel) is { } product)
            {
                return $"A projection adds or subtracts a product of two event values (ESM v29) that cannot be carried on the event: {product}.";
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
                scope.Joins.Any(join => join.Mappings.Any(mapping => !MappingSupported(mapping))) ||
                scope.Children.Any(child => !ChildrenSupported(plan, specification, child, readModel.Properties, identifier.Type)) ||
                (scope.Every is { } every && every.Mappings.Any(mapping => mapping.Operation != SemanticProjectionOperation.Set ||
                    mapping.Target.Length != 1 || mapping.Source is not (SemanticProjectionEventSourceIdentity or SemanticProjectionLiteral))) ||
                scope.From.Any(from => from.ParentKey is not null || !FromKeySupported(plan, specification, from, identifier.Type) ||
                    from.Mappings.Any(mapping => !MappingSupported(mapping))))
            {
                return "This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.";
            }
            return scope.Joins.Any(join => !JoinIdentityMatches(plan, specification, join, readModel.Properties, childIdentity: null)) ||
                scope.Children.Any(child => !ChildJoinIdentitiesMatch(plan, specification, child, readModel.Properties))
                ? JoinIdentityRefusal
                : null;
        }

        if (readModel.Properties.Any(property => property.Type.IsCollection || !IsScalar(property.Type)) || projection.Transitions.IsEmpty || projection.Transitions.Select(transition => transition.EventContract).Distinct().Count() != projection.Transitions.Length ||
            projection.Transitions.Any(transition => !FlatMatches(plan, specification, transition, readModel)))
        {
            return "A flat transition must use scalar event mappings, one transition per event and a key that is a required scalar event property of the identifier's value type.";
        }
        return null;
    }

    static string? ProductRejection(SemanticExecutionPlan plan, SemanticProjectionScope scope, SemanticReadModel readModel)
    {
        var context = new SemanticApplicationContext(plan);
        foreach (var from in scope.From)
        {
            foreach (var mapping in from.Mappings.Where(mapping => mapping.Factor is not null))
            {
                if (SemanticProjectionProducts.Rejection(mapping, true, plan.Events[from.EventContract], readModel.Properties, context, out _) is { } reason)
                {
                    return reason;
                }
            }
        }

        return null;
    }

    static bool HasProductOutsideFrom(SemanticProjectionScope scope) =>
        scope.Joins.Any(join => join.Mappings.Any(mapping => mapping.Factor is not null)) ||
        scope.Children.Any(child => HasProduct(child.Scope));

    static bool HasProduct(SemanticProjectionScope scope) =>
        scope.From.Any(from => from.Mappings.Any(mapping => mapping.Factor is not null)) ||
        scope.Joins.Any(join => join.Mappings.Any(mapping => mapping.Factor is not null)) ||
        scope.Children.Any(child => HasProduct(child.Scope));

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

    // Stage lowers every event-source identity to text, so Chronicle's join would match Text "1" with Number 1,
    // while the reference keeps them distinct. Admit a join only when its event's identities have the joined key's type.
    static bool JoinIdentityMatches(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionJoin join, System.Collections.Immutable.ImmutableArray<SemanticProperty> properties, SemanticId? childIdentity) =>
        join.Key is null &&
        (childIdentity is null || join.On == childIdentity) &&
        properties.SingleOrDefault(property => property.Id == join.On) is { Type: { IsCollection: false } onType } &&
        IsScalar(onType) &&
        Primitive(plan, onType) is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid &&
        ScopedKeysMatch(plan, specification, join.EventContract, onType);

    static bool ChildJoinIdentitiesMatch(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionChildren child, System.Collections.Immutable.ImmutableArray<SemanticProperty> parentProperties)
    {
        var collection = parentProperties.SingleOrDefault(property => property.Id == child.Property);
        if (collection is null) return false;
        var properties = plan.Model.Application.Types.Single(type => type.Id == collection.Type.Target).Properties;
        return child.Scope.Joins.All(join => JoinIdentityMatches(plan, specification, join, properties, child.IdentifiedBy)) &&
            child.Scope.Children.All(nested => ChildJoinIdentitiesMatch(plan, specification, nested, properties));
    }

    static bool MappingSupported(SemanticProjectionMapping mapping)
    {
        if (mapping.Target.Length != 1) return false;
        if (mapping.Factor is not null) return mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract && mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 };
        return mapping.Operation switch
        {
            SemanticProjectionOperation.Set => mapping.Source is SemanticProjectionEventProperty or SemanticProjectionEventSourceIdentity or SemanticProjectionLiteral,
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
        Primitive(plan, source) == Primitive(plan, identifier) ||
        (Primitive(plan, source) == SemanticPrimitiveType.Text && Primitive(plan, identifier) == SemanticPrimitiveType.Uuid);

    static bool FlatMatches(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjectionTransition transition, SemanticReadModel readModel)
    {
        if (!plan.Events.TryGetValue(transition.EventContract, out var @event) ||
            transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
            transition.AffectedInstance.Key is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } key)
        {
            return false;
        }

        var identifier = readModel.Properties.Single(property => property.IsIdentifier);
        return @event.Properties.Any(property => property.Id == key.Target) &&
            ChronicleEventPropertyNameIsSafe.Check(@event.Properties.Single(property => property.Id == key.Target).Name) &&
            @event.Properties.All(property => ChronicleEventPropertyNameIsSafe.Check(property.Name)) &&
            ChronicleReadModelPropertyNamesAreSafe.Check(readModel.Properties) &&
            transition.Mappings.Length == readModel.Properties.Length &&
            readModel.Properties.All(target => transition.Mappings.Count(mapping => mapping.TargetProperty == target.Id) == 1) &&
            transition.Mappings.All(mapping => mapping.Source switch
            {
                SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } source =>
                    @event.Properties.Any(property => property.Id == source.Target) &&
                    (mapping.TargetProperty != identifier.Id || source.Target == key.Target),
                SemanticValueExpression { Value: SemanticTextValue or SemanticNumberValue or SemanticBooleanValue } => mapping.TargetProperty != identifier.Id,
                _ => false
            }) &&
            @event.Properties.Single(property => property.Id == key.Target) is { Type: { IsOptional: false, IsCollection: false } } keyProperty && IsScalar(keyProperty.Type) &&
            SourceTypeMatches(plan, keyProperty.Type, identifier.Type) &&
            ScopedKeysMatch(plan, specification, transition.EventContract, identifier.Type, key.Target);
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

        // An event a capture appends takes the capture's key as its event source: the named field of the event it observes.
        foreach (var capture in plan.Captures.Values.Where(capture => capture.EventsSource is not null && capture.Appends.Any(append => append.EventContract == eventContract)))
        {
            var observations = specification.GivenEvents.Select(given => (given.EventContract, given.Values))
                .Concat(specification.WhenAppended is { } trigger ? [(trigger.EventContract, trigger.Values)] : []);
            foreach (var observed in observations.Where(fact => capture.EventsSource!.Events.Contains(fact.EventContract)))
            {
                var field = plan.Events[observed.EventContract].Properties.SingleOrDefault(property => string.Equals(property.Name, capture.Key, StringComparison.Ordinal));
                var value = field is null ? null : observed.Values.SingleOrDefault(candidate => candidate.TargetProperty == field.Id)?.Value;
                if (field is null || value is null || !Matches(new(field.Type, value)))
                {
                    return false;
                }
            }
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

    static bool SameScalar(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticTextValue a, SemanticTextValue b) => a.Value == b.Value,
        (SemanticNumberValue a, SemanticNumberValue b) => a.Value == b.Value,
        (SemanticBooleanValue a, SemanticBooleanValue b) => a.Value == b.Value,
        _ => false
    };
}
