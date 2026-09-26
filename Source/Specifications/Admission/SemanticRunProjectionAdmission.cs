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
        foreach (var projection in plan.Projections.Values.Where(value =>
            specification.ThenReadModels.Any(state => state.ReadModel == value.ReadModel) ||
            specification.ThenQueries.Any(query => plan.Queries.TryGetValue(query.Query, out var target) && target.ReadModel == value.ReadModel) ||
            (value.Scope is not null ? reachable.Count > 0 : value.Transitions.Any(transition => reachable.Contains(transition.EventContract)))))
        {
            var reason = Rejection(plan, projection, plan.ReadModels[projection.ReadModel], specification);
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

    static string? Rejection(SemanticExecutionPlan plan, SemanticProjection projection, SemanticReadModel readModel, SemanticSpecification specification)
    {
        if (readModel.Properties.Count(property => property.IsIdentifier) != 1 || readModel.Properties.Any(property =>
            property.Type.IsCollection || property.Type.IsOptional || property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)))
        {
            return "Per-run projections currently require a scalar read model with one identifier and no optional properties.";
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

        if (projection.Transitions.Length == 0 || projection.Transitions.Select(transition => transition.EventContract).Distinct().Count() != projection.Transitions.Length ||
            projection.Transitions.Any(transition => !plan.Events.TryGetValue(transition.EventContract, out var @event) ||
                transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
                transition.AffectedInstance.Key is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } key ||
                !@event.Properties.Any(property => property.Id == key.Target) ||
                transition.Mappings.Any(mapping => mapping.TargetProperty == readModel.Properties.Single(property => property.IsIdentifier).Id &&
                    (mapping.Source is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } identifier || identifier.Target != key.Target)) ||
                transition.Mappings.Any(mapping => mapping.Source is not SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } source ||
                    !@event.Properties.Any(property => property.Id == source.Target) || !readModel.Properties.Any(property => property.Id == mapping.TargetProperty)) ||
                !KeyMatchesSource(plan, specification, transition, key.Target)))
        {
            return "A flat transition must use scalar event mappings and a key equal to the fact's event source.";
        }
        return null;
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
