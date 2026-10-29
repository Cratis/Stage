// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool HasProjectedSeedExpectation(SemanticApplicationContext context, SemanticSpecification specification) =>
        !specification.ThenAbsentReadModels.IsEmpty || specification.GivenReadModels.All(seed =>
            context.ReadModels.TryGetValue(seed.ReadModel, out var model) && model.Properties.Count(property => property.IsIdentifier) > 1);

    static bool CanSeedProjection(SemanticApplicationContext context, SemanticSpecificationReadModel seed)
    {
        if (!context.ReadModels.TryGetValue(seed.ReadModel, out var model)) return false;
        return model.Properties.Count(property => property.IsIdentifier) > 1
            ? CanSeedCompositeProjection(context, model, seed)
            : CanSeedScalarProjection(context, seed);
    }

    static bool CanSeedCompositeProjection(SemanticApplicationContext context, SemanticReadModel model, SemanticSpecificationReadModel seed) =>
        HasCompleteSeedValues(model, seed) && CompositeSeedKeyMatches(model, seed) && HasSimpleSeedProjection(context, model);

    static bool HasCompleteSeedValues(SemanticReadModel model, SemanticSpecificationReadModel seed) =>
        ValuesMatch(seed.Values, model.Properties) && seed.Values.All(value => IsScalar(value.Value)) &&
        model.Properties.All(property => !property.Type.IsOptional && !property.Type.IsCollection &&
            property.Type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept);

    static bool CompositeSeedKeyMatches(SemanticReadModel model, SemanticSpecificationReadModel seed)
    {
        if (seed.Key is not SemanticCompositeValue key) return false;
        var identifiers = model.Properties.Where(property => property.IsIdentifier).ToArray();
        return key.Properties.Length == identifiers.Length &&
            key.Properties.Select(part => part.TargetProperty).Distinct().Count() == identifiers.Length &&
            identifiers.All(identifier => CompositeSeedPartMatches(identifier.Id, key, seed.Values));
    }

    static bool CompositeSeedPartMatches(SemanticId property, SemanticCompositeValue key, ImmutableArray<SemanticPropertyValue> values)
    {
        var part = key.Properties.SingleOrDefault(part => part.TargetProperty == property);
        return part is not null && Equals(values.Single(value => value.TargetProperty == property).Value, part.Value);
    }

    static bool HasSimpleSeedProjection(SemanticApplicationContext context, SemanticReadModel model)
    {
        if (context.Reducers.Any(reducer => reducer.ReadModel == model.Id)) return false;
        var projections = context.Projections.Values.Where(projection => projection.ReadModel == model.Id).ToArray();
        if (projections.Length != 1 || projections[0].Scope is not { } scope) return false;
        var identifiers = model.Properties.Where(property => property.IsIdentifier).ToArray();
        return IsFlatSeedScope(scope) &&
            scope.From.All(from => SemanticCompositeKeyAdmission.KeyCoversParts(from.Key, from.ParentKey, identifiers)) &&
            scope.Removals.All(removal => SemanticCompositeKeyAdmission.KeyCoversParts(removal.Key, removal.ParentKey, identifiers));
    }

    static bool IsFlatSeedScope(SemanticProjectionScope scope) =>
        scope.Every is null && scope.Joins.IsEmpty && scope.JoinRemovals.IsEmpty && scope.Children.IsEmpty && scope.Nested.IsEmpty;
}
