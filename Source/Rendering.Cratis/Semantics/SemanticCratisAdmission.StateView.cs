// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits the supported state-view shape.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateStateView(
        SemanticApplicationContext context,
        SemanticSlice slice,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if ((slice.ReadModels.Length == 0 && slice.Reducers.IsEmpty) ||
            slice.Projections.Any(projection => !slice.ReadModels.Any(model => model.Id == projection.ReadModel)) ||
            slice.Reducers.Any(reducer => !slice.ReadModels.Any(model => model.Id == reducer.ReadModel)) ||
            slice.ReadModels.Any(model => slice.Projections.Count(_ => _.ReadModel == model.Id) +
                slice.Reducers.Count(_ => _.ReadModel == model.Id) != 1))
        {
            diagnostics.Add(Error("STAGE-ESM-007", $"State-view slice '{slice.Name}' needs exactly one projection for each read model.", slice.Id));
            return;
        }

        foreach (var readModel in slice.ReadModels)
        {
            var projection = slice.Projections.SingleOrDefault(_ => _.ReadModel == readModel.Id);
            if (readModel.Properties.Any(_ => !TypeExists(context, _.Type)) || readModel.Properties.Count(_ => _.IsIdentifier) != 1)
            {
                diagnostics.Add(projection is null
                    ? Error("STAGE-ESM-008", $"Read model '{readModel.Name}' needs resolvable properties and one identifier.", readModel.Id)
                    : Error("STAGE-ESM-008", $"Projection '{projection.Name}' needs a resolvable read model with one identifier.", projection.Id));
                continue;
            }

            // Every read model renders as a record with its queries, whether a projection or a reducer
            // builds it, and both are persisted through Chronicle's sinks. So these checks run before
            // the projection-specific ones below.
            if (!ChronicleReadModelPropertyNamesAreSafe.Check(readModel.Properties))
            {
                diagnostics.Add(projection is null
                    ? Error("STAGE-ESM-017", $"Read model '{readModel.Name}' cannot render: a property name collides with Chronicle's property paths or another generated property.", readModel.Id)
                    : Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: a read-model property name collides with Chronicle's property paths or another generated property.", projection.Id));
                continue;
            }

            if (!ReadModelQueryNamesAreUnique(context, slice, readModel))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Read model '{readModel.Name}' has property or query names that collide in generated C#.", readModel.Id));
                continue;
            }

            if (projection is null)
            {
                continue;
            }

            if (!ProjectionReferencedEventNamesAreUnique.Check(projection, context.Events))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Projection '{projection.Name}' references an event with property names that collide in generated C#.", projection.Id));
                continue;
            }

            if (projection.Scope is { } scope)
            {
                if (!GeneratedPascalCase.ProjectionTypeNameIsSafe(projection.Name))
                {
                    diagnostics.Add(Error("STAGE-ESM-012", $"Projection '{projection.Name}' has a name that collides with its generated C# members.", projection.Id));
                    continue;
                }

                var rejection = SemanticScopedProjectionSupport.Rejection(scope, context, readModel.Properties);
                if (rejection is not null || !projection.Transitions.IsEmpty)
                {
                    diagnostics.Add(Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: {rejection ?? "A scope cannot also have flat transitions."}", projection.Id));
                }

                continue;
            }

            if (projection.Transitions.Length != 1)
            {
                diagnostics.Add(Error("STAGE-ESM-008", $"Projection '{projection.Name}' does not have one resolvable read-model transition.", projection.Id));
                continue;
            }

            var transition = projection.Transitions[0];
            if (context.Events.TryGetValue(transition.EventContract, out var sourceEvent) &&
                sourceEvent.Properties.Any(property => !ChronicleEventPropertyNameIsSafe.Check(property.Name)))
            {
                diagnostics.Add(Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: an event property name collides with Chronicle expression syntax.", projection.Id));
                continue;
            }

            if (!context.Events.TryGetValue(transition.EventContract, out var @event) ||
                transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
                !IsProperty(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, @event.Properties.Select(_ => _.Id)) ||
                !SemanticFlatProjectionSupport.MappingsMatch(transition.Mappings, readModel.Properties, @event.Properties, SemanticExpressionRootKind.Event) ||
                !UsesEventSourceIdentity(context, transition, @event, readModel))
            {
                diagnostics.Add(Error("STAGE-ESM-009", $"Projection '{projection.Name}' cannot preserve its affected instance with model-bound Cratis projection semantics.", projection.Id));
            }
        }

        foreach (var query in slice.Queries.Where(query => ValidateQueryAuthorization(context, query, diagnostics)))
        {
            var readModel = slice.ReadModels.SingleOrDefault(_ => _.Id == query.ReadModel);
            if (!QueryFiltersAreRenderable(context, query, readModel, diagnostics))
            {
                diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' is neither an optional snapshot lookup by identifier nor a collection query the renderer can preserve.", query.Id));
            }
        }
    }

    static bool ReadModelQueryNamesAreUnique(SemanticApplicationContext context, SemanticSlice slice, SemanticReadModel readModel)
    {
        var queries = slice.Queries.Where(query => query.ReadModel == readModel.Id).ToArray();
        var types = new SemanticTypeSystem(context);

        return GeneratedPascalCase.ReadModelMembersAreUnique(readModel.Name, readModel.Properties.Select(property => property.Name), queries.Select(query => query.Name)) &&
            GeneratedPascalCase.QueriesAreUnique(queries.Where(query => query.Argument is null || TypeExists(context, query.Argument.Type))
                .Select(query => (query.Name, QueryParameterTypes(query, types), query.Argument?.Name ?? string.Empty))) &&
            queries.All(query => GeneratedPascalCase.QueryParametersAreUnique(QueryParameterNames(query)));
    }

    static IEnumerable<string> QueryParameterNames(SemanticKeyedQuery query) =>
        (query.Argument is null ? Enumerable.Empty<string>() : [query.Argument.Name]).Concat(query.Filters.Select(filter => filter.Name));

    static string QueryParameterTypes(SemanticKeyedQuery query, SemanticTypeSystem types) =>
        string.Join(',', (query.Argument is null ? Enumerable.Empty<string>() : [types.Type(query.Argument.Type)])
            .Concat(query.Filters.Select(filter => types.Type(filter.Type))));

    static bool QueryFiltersAreRenderable(SemanticApplicationContext context, SemanticKeyedQuery query, SemanticReadModel? readModel, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (query.Argument?.Type is { IsOptional: true } || query.Filters.Any(filter =>
            filter.Type is { IsOptional: true, Kind: SemanticTypeReferenceKind.Concept } &&
            context.Concepts[filter.Type.Target].Validations.Any(rule => rule.Kind == SemanticValidationRuleKind.NotEmpty)))
        {
            diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' cannot preserve nullable keys or optional concept filters with not-empty rules: Arc skips validation for null arguments while the reference validates supplied null concepts.", query.Id));
            return false;
        }

        if (IsSnapshotLookup(context, query, readModel)) return query.Filters.IsDefaultOrEmpty;
        if (!IsCollection(context, query, readModel)) return false;

        var optionalSeen = false;
        foreach (var filter in query.Filters)
        {
            if (!FilterHasMatchingScalarProperty(context, filter, readModel!) || !FilterPreservesParameterOrder(filter, optionalSeen))
            {
                diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' cannot preserve equality filter '{filter.Name}' as a scalar parameter in declaration order.", query.Id));
                return false;
            }

            optionalSeen |= filter.Type.IsOptional;
        }

        return true;
    }

    static bool IsSnapshotLookup(SemanticApplicationContext context, SemanticKeyedQuery query, SemanticReadModel? readModel)
    {
        var identifiers = readModel?.Properties.Where(property => property.IsIdentifier).ToArray() ?? [];

        return query is { Cardinality: SemanticQueryCardinality.ZeroOrOne, Delivery: SemanticQueryDelivery.Snapshot, Argument: { } argument } &&
            identifiers is [var identifier] && query.KeyProperty == identifier.Id && TypeExists(context, argument.Type);
    }

    static bool IsCollection(SemanticApplicationContext context, SemanticKeyedQuery query, SemanticReadModel? readModel) =>
        (readModel, query) is ({ } model, { Cardinality: SemanticQueryCardinality.Many, Delivery: SemanticQueryDelivery.Snapshot or SemanticQueryDelivery.Live }) &&
        (query.Argument is not { } argument || (TypeExists(context, argument.Type) && model.Properties.Any(property => property.Id == query.KeyProperty)));

    static bool FilterHasMatchingScalarProperty(SemanticApplicationContext context, SemanticQueryFilter filter, SemanticReadModel readModel) =>
        readModel.Properties.SingleOrDefault(property => property.Id == filter.Property) is { } property &&
        FilterTypeIsRenderable(context, filter.Type) && property.Type with { IsOptional = false } == filter.Type with { IsOptional = false };

    static bool FilterTypeIsRenderable(SemanticApplicationContext context, SemanticTypeReference type) =>
        TypeExists(context, type) && !IsNested(type) && SemanticValidationRendering.UnderlyingPrimitive(type, context) != SemanticPrimitiveType.DateTime;

    static bool FilterPreservesParameterOrder(SemanticQueryFilter filter, bool optionalSeen) => !optionalSeen || filter.Type.IsOptional;

    static bool IsNested(SemanticTypeReference type) => type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType;
}
