// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks keyed absence assertions against the projection scenario's materialized instances.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    const string AbsenceRejection = "Read-model absence requires a projection with a scalar text or UUID identifier and faithful event replay; seeds must be complete scalar records for independent projections, and reducers and ambiguous unordered duplicate productions are not supported.";

    static bool CanAssertAbsence(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticSpecificationAbsentReadModel expected) =>
        context.ReadModels.TryGetValue(expected.ReadModel, out var readModel) &&
        readModel.Properties.Count(property => property.IsIdentifier) == 1 &&
        IsScalar(expected.Key) && IsLosslessEventSource(context, readModel.Properties.Single(property => property.IsIdentifier).Type) &&
        context.Projections.Values.Any(projection => projection.ReadModel == expected.ReadModel) &&
        !context.Reducers.Any(reducer => reducer.ReadModel == expected.ReadModel) &&
        CanSeedProjectedAbsence(context, specification) &&
        (!specification.ThenEventsInAnyOrder || specification.ThenEvents.Select(expected => expected.EventContract).Distinct().Count() == specification.ThenEvents.Length);
    static bool CanSeedProjectedAbsence(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.GivenReadModels.IsEmpty)
        {
            return true;
        }
        if (!HasProjectedSeedExpectation(context, specification) || !specification.ThenQueries.IsEmpty ||
            (specification.When is { } when && !context.Commands[when.Command].Reads.IsDefaultOrEmpty))
        {
            return false;
        }
        return specification.GivenReadModels.All(seed => CanSeedProjection(context, seed));
    }

    static bool CanSeedScalarProjection(SemanticApplicationContext context, SemanticSpecificationReadModel seed)
    {
        if (!context.ReadModels.TryGetValue(seed.ReadModel, out var model) ||
            model.Properties.Count(property => property.IsIdentifier) != 1 ||
            model.Properties.Any(property => property.Type.IsCollection || property.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)) ||
            !IsScalar(seed.Key) || !IsLosslessEventSource(context, model.Properties.Single(property => property.IsIdentifier).Type) ||
            !ValuesMatch(seed.Values, model.Properties) || seed.Values.Any(value => !IsScalar(value.Value)) ||
            !Equals(seed.Values.Single(value => value.TargetProperty == model.Properties.Single(property => property.IsIdentifier).Id).Value, seed.Key) ||
            context.Reducers.Any(reducer => reducer.ReadModel == seed.ReadModel))
        {
            return false;
        }
        var projections = context.Projections.Values.Where(projection => projection.ReadModel == seed.ReadModel).ToArray();
        return projections.Length == 1 && (projections[0].Scope is not { } scope ||
            (scope.Joins.IsEmpty && scope.JoinRemovals.IsEmpty && scope.Children.IsEmpty && scope.Nested.IsEmpty &&
                scope.From.All(from => from.ParentKey is null && CanReplaySeedKey(from.Key)) &&
                scope.Removals.All(removal => removal.ParentKey is null && CanReplaySeedRemoval(context, removal, model.Properties.Single(property => property.IsIdentifier).Type))));
    }

    static bool CanReplaySeedRemoval(SemanticApplicationContext context, SemanticProjectionRemoval removal, SemanticTypeReference identifier)
    {
        if (removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity })
        {
            return true;
        }
        return removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } } &&
            context.Events[removal.EventContract].Properties.SingleOrDefault(candidate => candidate.Id == id) is { Type: { IsOptional: false, IsCollection: false } } property &&
            IsLosslessEventSource(context, property.Type) && SeedKeyPrimitive(context, property.Type) == SeedKeyPrimitive(context, identifier);
    }

    static SemanticPrimitiveType SeedKeyPrimitive(SemanticApplicationContext context, SemanticTypeReference type) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;

    static bool CanReplaySeedKey(SemanticProjectionKey key) => key is SemanticProjectionValueKey
    {
        Value: SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty { Path.Length: 1 } or SemanticProjectionLiteral
    };
}
