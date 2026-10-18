// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

internal static class SemanticRunProjectionAdmission
{
    internal static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> reachableEvents) =>
        RequiredLookupRefusal(plan, specification) ?? CheckProjections(plan, specification, reachableEvents);

    internal static SemanticUnsupportedCapability? CheckFact(SemanticExecutionPlan plan, SemanticSpecificationEvent fact)
    {
        // Reuse the same concrete source/key checks as fixture admission before a reaction fact is appended.
        var fixture = new SemanticSpecification(fact.EventContract, "Reaction production", [fact], [], null, [], [], [], []);

        return Check(plan, fixture, [fact.EventContract]);
    }

    internal static string QueryType(SemanticTypeReference type, SemanticExecutionPlan plan) =>
        type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive.ToString() + (type.IsOptional ? "?" : string.Empty) + (type.IsCollection ? "[]" : string.Empty),
            SemanticTypeReferenceKind.Concept => GeneratedPascalCase.From(plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Name),
            SemanticTypeReferenceKind.CompositeType => GeneratedPascalCase.From(plan.Model.Application.Types.Single(composite => composite.Id == type.Target).Name),
            _ => type.ToString() ?? string.Empty
        };

    internal static bool QueryMembersAreUnique(SemanticReadModel readModel, IEnumerable<SemanticKeyedQuery> candidates, SemanticExecutionPlan plan)
    {
        var queries = candidates.Where(query => query.ReadModel == readModel.Id).ToArray();

        return GeneratedPascalCase.ReadModelMembersAreUnique(readModel.Name, readModel.Properties.Select(property => property.Name), queries.Select(query => query.Name)) &&
            GeneratedPascalCase.QueriesAreUnique(queries.Select(query => (query.Name, QueryParameterTypes(query, plan), query.Argument?.Name ?? string.Empty))) &&
            queries.All(QueryParametersAreUnique);
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
        if (!emptyQuery && FilteredQueryRejection(specification) is { } filtered) return filtered;

        var reachable = reachableEvents.ToHashSet();
        var requested = plan.Projections.Values.Where(value =>
            specification.ThenReadModels.Any(state => state.ReadModel == value.ReadModel) ||
            specification.ThenAbsentReadModels.Any(state => state.ReadModel == value.ReadModel) ||
            specification.ThenQueries.Any(query => plan.Queries.TryGetValue(query.Query, out var target) && target.ReadModel == value.ReadModel) ||
            (value.Scope is not null ? reachable.Count > 0 : value.Transitions.Any(transition => reachable.Contains(transition.EventContract)))).ToArray();
        var duplicate = requested.Select(projection => projection.ReadModel).FirstOrDefault(id => plan.Projections.Values.Count(projection => projection.ReadModel == id) > 1);
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

        var assertedModels = specification.ThenReadModels.Select(assertion => assertion.ReadModel)
            .Concat(specification.ThenAbsentReadModels.Select(assertion => assertion.ReadModel));
        foreach (var readModel in assertedModels)
        {
            if (!plan.ReadModels.ContainsKey(readModel) || plan.Projections.Values.All(projection => projection.ReadModel != readModel))
            {
                return new(StageExecutionCapability.Projection, readModel.ToString(), "The asserted read model has no projection in this run.");
            }
        }
        foreach (var assertion in specification.ThenQueries)
        {
            if (emptyQuery && plan.Queries.ContainsKey(assertion.Query)) continue;
            if (!plan.Queries.TryGetValue(assertion.Query, out var query) ||
                query.Delivery != SemanticQueryDelivery.Snapshot || query.Cardinality != SemanticQueryCardinality.ZeroOrOne ||
                !plan.ReadModels.TryGetValue(query.ReadModel, out var model) ||
                model.Properties.Where(property => property.IsIdentifier).Select(property => (SemanticId?)property.Id).ToArray() is not [var identifier] || identifier != query.KeyProperty ||
                plan.Projections.Values.All(projection => projection.ReadModel != query.ReadModel))
            {
                return new(StageExecutionCapability.Query, assertion.Query.ToString(), "Only optional snapshot queries by read-model identifier are admitted.");
            }
        }
        return null;
    }

    static string QueryParameterTypes(SemanticKeyedQuery query, SemanticExecutionPlan plan) =>
        string.Join(',', (query.Argument is null ? Enumerable.Empty<string>() : [QueryType(query.Argument.Type, plan)])
            .Concat(query.Filters.Select(filter => QueryType(filter.Type, plan))));

    static bool QueryParametersAreUnique(SemanticKeyedQuery query) =>
        GeneratedPascalCase.QueryParametersAreUnique((query.Argument is null ? Enumerable.Empty<string>() : [query.Argument.Name])
            .Concat(query.Filters.Select(filter => filter.Name)));

    static SemanticUnsupportedCapability? FilteredQueryRejection(SemanticSpecification specification)
    {
        var filtered = specification.ThenQueries.FirstOrDefault(assertion => !assertion.Filters.IsDefaultOrEmpty);

        return filtered is null ? null : new(StageExecutionCapability.Query, filtered.Query.ToString(), "STAGE-ESM-010: Equality query filter assertions cannot be executed by the per-run projection scenario.");
    }

    static string? Rejection(SemanticExecutionPlan plan, SemanticProjection projection, SemanticReadModel readModel, SemanticSpecification specification, HashSet<SemanticId> reachable)
    {
        if (!ProjectionReferencedEventNamesAreUnique.Check(projection, plan.Events))
        {
            return "A projection references an event with property names that collide in generated C#.";
        }
        if (ProjectionReferencedEventNamesAreUnique.Contracts(projection)
            .Any(id => plan.Events.TryGetValue(id, out var @event) &&
                @event.Properties.Any(property => !ChronicleEventPropertyNameIsSafe.Check(property.Name))))
        {
            return "An event property name collides with Chronicle expression syntax.";
        }
        if (readModel.Properties.Count(property => property.IsIdentifier) > 1)
        {
            return $"Read model '{readModel.Name}' has several key parts (ESM v27 composite read-model keys); the per-run projection scenario seeds and looks up instances by one event-source key, so it cannot run composite read-model keys yet.";
        }
        if (readModel.Properties.Count(property => property.IsIdentifier) != 1 || readModel.Properties.Any(property =>
            property.Type.IsCollection || property.Type.IsOptional || property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)))
        {
            return "Per-run projections currently require a scalar read model with one identifier and no optional properties.";
        }
        if (readModel.Properties.Any(ChronicleReadModelPropertyNamesAreSafe.CollidesWithRootDocumentKey))
        {
            return "A non-identifier read-model property named Id collides with Chronicle's document key.";
        }
        if (!ChronicleReadModelPropertyNamesAreSafe.Check(readModel.Properties))
        {
            return "A read-model property name collides with Chronicle's property paths or another generated property.";
        }
        if (!QueryMembersAreUnique(readModel, plan.Queries.Values, plan))
        {
            return "A read-model property or query name collides in generated C#.";
        }
        if (readModel.Properties.Any(property => Primitive(plan, property.Type) == SemanticPrimitiveType.DateTime))
        {
            return "DateTime read-model values cannot preserve the reference's round-trip text in the per-run projection.";
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
            if (!GeneratedPascalCase.ProjectionTypeNameIsSafe(projection.Name))
            {
                return "A projection type name collides with its generated C# members.";
            }

            var rejection = SemanticScopedProjectionSupport.Rejection(scope, new SemanticApplicationContext(plan), readModel.Properties);
            if (rejection is not null)
            {
                return rejection;
            }
            if (readModel.Properties.Length == 1 && scope.From.Any(from => from.Mappings.IsEmpty))
            {
                return "A mapping-free identifier-only from block does not materialize a Chronicle read model.";
            }
            if (scope.Every is { SubscribesToAllEvents: true }
                ? reachable.Any(eventContract => !ScopedKeysMatch(plan, specification, eventContract, identifier.Type))
                : scope.From.Select(from => from.EventContract).Concat(scope.Removals.Select(removal => removal.EventContract))
                    .Any(eventContract => reachable.Contains(eventContract) && !ScopedKeysMatch(plan, specification, eventContract, identifier.Type)))
            {
                return "A scoped event-source key must have the read-model identifier's value type and enumeration values.";
            }
            if (projection.Transitions.Length > 0 || scope.Joins.Length > 0 ||
                scope.Removals.Any(removal => removal.ParentKey is not null || removal.Key is not SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) ||
                scope.JoinRemovals.Length > 0 || scope.Children.Length > 0 || scope.Nested.Length > 0 ||
                (scope.Every is { } every && every.Mappings.Any(mapping => mapping.Operation != SemanticProjectionOperation.Set ||
                    mapping.Target.Length != 1 || mapping.Source is not SemanticProjectionEventSourceIdentity)) ||
                scope.From.Any(from => from.ParentKey is not null || from.Key is not SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } ||
                    from.Mappings.Any(mapping => mapping.Operation != SemanticProjectionOperation.Set || mapping.Target.Length != 1 ||
                        mapping.Source is not (SemanticProjectionEventProperty { Path.Length: 1 } or SemanticProjectionEventSourceIdentity))))
            {
                return "This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.";
            }
            return null;
        }

        if (projection.Transitions.Length != 1 || !FlatMatches(plan, specification, projection.Transitions[0], readModel))
        {
            return "A flat transition must use scalar event mappings and a key equal to the fact's event source.";
        }
        return null;
    }

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

    static bool ScopedKeysMatch(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticId eventContract, SemanticTypeReference identifier)
    {
        bool Matches(SemanticEventSourceIdentity? source) => source is not null &&
            source.Type.Kind == identifier.Kind && source.Type.Target == identifier.Target && source.Type.Primitive == identifier.Primitive &&
            source.Value is SemanticTextValue text &&
            (Primitive(plan, identifier) != SemanticPrimitiveType.Uuid ||
                (Guid.TryParseExact(text.Value, "D", out var uuid) && uuid.ToString("D") == text.Value)) &&
            (identifier.Kind != SemanticTypeReferenceKind.Concept ||
                plan.Model.Application.Concepts.Single(concept => concept.Id == identifier.Target).Values is not { IsEmpty: false } values || values.Contains(text.Value));

        if (specification.GivenEvents.Any(given => given.EventContract == eventContract && !Matches(given.EventSource)) ||
            (specification.WhenAppended is { } appended && appended.EventContract == eventContract && !Matches(appended.EventSource)))
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
            var destination = produced.Destination ?? command.Destination?.Value;
            var value = destination switch
            {
                SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } property =>
                    when.Values.SingleOrDefault(input => input.TargetProperty == property.Target)?.Value,
                SemanticValueExpression literal => literal.Value,
                null => when.EventSource?.Value,
                _ => null
            };
            var type = destination is SemanticResolvedExpression resolved
                ? command.Properties.Single(property => property.Id == resolved.Target).Type : command.Destination?.Type ?? when.EventSource?.Type;
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
