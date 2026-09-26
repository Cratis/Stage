// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Specifications.Admission;

internal static class SemanticRunProjectionAdmission
{
    internal static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification, IEnumerable<SemanticId> reachableEvents)
    {
        var reachable = reachableEvents.ToHashSet();
        var requested = plan.Projections.Values.Where(value =>
            specification.ThenReadModels.Any(state => state.ReadModel == value.ReadModel) ||
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

        foreach (var assertion in specification.ThenReadModels)
        {
            if (!plan.ReadModels.ContainsKey(assertion.ReadModel) || plan.Projections.Values.All(projection => projection.ReadModel != assertion.ReadModel))
            {
                return new(StageExecutionCapability.Projection, assertion.ReadModel.ToString(), "The asserted read model has no projection in this run.");
            }
        }
        foreach (var assertion in specification.ThenQueries)
        {
            if (!plan.Queries.TryGetValue(assertion.Query, out var query) || query.Authorization is not null ||
                query.Delivery != SemanticQueryDelivery.Snapshot || query.Cardinality != SemanticQueryCardinality.ZeroOrOne ||
                !plan.ReadModels.TryGetValue(query.ReadModel, out var model) ||
                model.Properties.SingleOrDefault(property => property.IsIdentifier)?.Id != query.KeyProperty ||
                plan.Projections.Values.All(projection => projection.ReadModel != query.ReadModel))
            {
                return new(StageExecutionCapability.Query, assertion.Query.ToString(), "Only unprotected, optional snapshot queries by read-model identifier are admitted.");
            }
        }
        return null;
    }

    static string? Rejection(SemanticExecutionPlan plan, SemanticProjection projection, SemanticReadModel readModel, SemanticSpecification specification, HashSet<SemanticId> reachable)
    {
        if (readModel.Properties.Count(property => property.IsIdentifier) != 1 || readModel.Properties.Any(property =>
            property.Type.IsCollection || property.Type.IsOptional || property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)))
        {
            return "Per-run projections currently require a scalar read model with one identifier and no optional properties.";
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
            var rejection = SemanticScopedProjectionSupport.Rejection(scope, new SemanticApplicationContext(plan), readModel.Properties);
            if (rejection is not null)
            {
                return rejection;
            }
            if (readModel.Properties.Length == 1 && scope.From.Any(from => from.Mappings.IsEmpty))
            {
                return "A mapping-free identifier-only from block does not materialize a Chronicle read model.";
            }
            if (scope.From.Any(from => reachable.Contains(from.EventContract) && !ScopedKeysMatch(plan, specification, from.EventContract, identifier.Type)))
            {
                return "A scoped event-source key must have the read-model identifier's value type and enumeration values.";
            }
            if (projection.Transitions.Length > 0 || scope.Joins.Length > 0 || scope.Removals.Length > 0 ||
                scope.JoinRemovals.Length > 0 || scope.Children.Length > 0 || scope.Nested.Length > 0 || scope.Every is not null ||
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
