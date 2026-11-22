// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Admits a whole flat projection only when every mapping and initialization source is representable.
/// </summary>
internal static class SemanticFlatModelBoundProjectionAdmission
{
    internal static bool Expresses(SemanticProjection projection, SemanticReadModel readModel, SemanticApplicationContext context)
    {
        if (projection.Scope is not { } scope || !ReadModelSupported(readModel, context)) return false;
        return ScopeSupported(scope, readModel, context) &&
            readModel.Properties.Where(property => !property.IsIdentifier).All(property => HasSource(scope, property, context));
    }

    static bool ScopeSupported(SemanticProjectionScope scope, SemanticReadModel readModel, SemanticApplicationContext context) =>
        Flat(scope) && UniqueEvents(scope) && scope.From.All(from => FromSupported(from, readModel, context)) &&
        RemovalsSupported(scope) && EverySupported(scope.Every) && JoinsSupported(scope, readModel);

    static bool ReadModelSupported(SemanticReadModel readModel, SemanticApplicationContext context) =>
        readModel.Properties.Count(property => property.IsIdentifier) == 1 &&
        !SemanticVersionFeatures.HasCompositeKey(readModel) && !context.Reducers.Any(reducer => reducer.ReadModel == readModel.Id);

    static bool Flat(SemanticProjectionScope scope) => scope.From.Length > 0 && scope.Nested.Length == 0 && scope.Children.Length == 0;

    static bool UniqueEvents(SemanticProjectionScope scope) =>
        scope.From.Select(from => from.EventContract).Distinct().Count() == scope.From.Length &&
        scope.Joins.Select(join => join.EventContract).Distinct().Count() == scope.Joins.Length;

    static bool FromSupported(SemanticProjectionFrom from, SemanticReadModel readModel, SemanticApplicationContext context) =>
        FromKeySupported(from.Key, readModel, context) && ParentSupported(from.ParentKey) &&
        from.Mappings.All(mapping => MappingSupported(mapping, readModel, context, context.Events[from.EventContract])) &&
        from.Mappings.Where(mapping => mapping.Operation != SemanticProjectionOperation.Clear)
            .GroupBy(mapping => mapping.Target[0]).All(group => group.Count() == 1);

    static bool FromKeySupported(SemanticProjectionKey key, SemanticReadModel readModel, SemanticApplicationContext context) =>
        SemanticFlatModelBoundProjectionValues.PathKey(key) ||
        (key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue } literal } &&
            SemanticFlatModelBoundProjectionValues.LiteralSupported(literal, readModel.Properties.Single(property => property.IsIdentifier), context));

    static bool ParentSupported(SemanticProjectionKey? key) => key is null or SemanticProjectionValueKey { Value: SemanticProjectionEventProperty };

    static bool RemovalsSupported(SemanticProjectionScope scope) =>
        scope.Removals.All(removal => ParentSupported(removal.ParentKey) && SemanticFlatModelBoundProjectionValues.PathKey(removal.Key)) &&
        scope.JoinRemovals.All(removal => SemanticFlatModelBoundProjectionValues.PathKey(removal.Key));

    static bool MappingSupported(SemanticProjectionMapping mapping, SemanticReadModel readModel, SemanticApplicationContext context, SemanticEventContract @event)
    {
        if (mapping.Target.Length != 1) return false;
        var property = readModel.Properties.Single(candidate => candidate.Id == mapping.Target[0]);
        if (mapping.Factor is not null) return ProductSupported(mapping, property, @event, context);
        return mapping.Operation switch
        {
            SemanticProjectionOperation.Set => SetSupported(mapping.Source, property, context),
            SemanticProjectionOperation.SetOnce => mapping.Source is SemanticProjectionEventProperty,
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract => mapping.Source is SemanticProjectionEventProperty || SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, property, context),
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Clear => property.Type.IsOptional,
            _ => false
        };
    }

    static bool SetSupported(SemanticProjectionValue? source, SemanticProperty property, SemanticApplicationContext context) => source switch
    {
        SemanticProjectionEventProperty or SemanticProjectionEventContextValue or SemanticProjectionEventSourceIdentity => true,
        SemanticProjectionLiteral literal => SemanticFlatModelBoundProjectionValues.LiteralSupported(literal, property, context),
        _ => false
    };

    static bool ProductSupported(SemanticProjectionMapping mapping, SemanticProperty property, SemanticEventContract @event, SemanticApplicationContext context) =>
        mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract &&
        SemanticProjectionProducts.Of(mapping, @event, [property], context) is not null;

    static bool EverySupported(SemanticProjectionEvery? every) => every is null ||
        (every.IncludeChildren && every.Mappings.Select(mapping => mapping.Target[0]).Distinct().Count() == every.Mappings.Length &&
            every.Mappings.All(mapping => mapping.Target.Length == 1 && mapping.Factor is null && mapping.Operation == SemanticProjectionOperation.Set &&
                mapping.Source is SemanticProjectionEventContextValue or SemanticProjectionEventSourceIdentity));

    static bool JoinsSupported(SemanticProjectionScope scope, SemanticReadModel readModel)
    {
        var otherTargets = scope.From.SelectMany(from => from.Mappings).Concat(scope.Every?.Mappings ?? []).SelectMany(mapping => mapping.Target).ToHashSet();
        return scope.Joins.All(join => DefaultJoinKey(join.Key) &&
            !scope.From.Any(from => from.EventContract == join.EventContract) &&
            join.Mappings.All(mapping => JoinMappingSupported(mapping, otherTargets)) &&
            (scope.From.Any(from => from.Mappings.Any(mapping => mapping.Target.SequenceEqual([join.On]))) || readModel.Properties.Single(property => property.Id == join.On).IsIdentifier));
    }

    static bool DefaultJoinKey(SemanticProjectionKey? key) => key is null or SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity };

    static bool JoinMappingSupported(SemanticProjectionMapping mapping, HashSet<SemanticId> otherTargets) =>
        mapping.Target.Length == 1 && !otherTargets.Contains(mapping.Target[0]) && mapping.Operation == SemanticProjectionOperation.Set &&
        mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 } && mapping.Factor is null;

    static bool HasSource(SemanticProjectionScope scope, SemanticProperty property, SemanticApplicationContext context) =>
        scope.From.Any(from => from.Mappings.Any(mapping => mapping.Target.SequenceEqual([property.Id]) && IsSource(mapping, property, context))) ||
        scope.Joins.Any(join => join.Mappings.Any(mapping => mapping.Target.SequenceEqual([property.Id]))) ||
        (scope.Every?.Mappings.Any(mapping => mapping.Target.SequenceEqual([property.Id])) ?? false);

    static bool IsSource(SemanticProjectionMapping mapping, SemanticProperty property, SemanticApplicationContext context) =>
        mapping.Operation != SemanticProjectionOperation.Clear && mapping.Source is not SemanticProjectionLiteral { Value: SemanticNullValue } &&
        !SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, property, context);
}
