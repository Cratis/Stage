// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Lifts the queries a performer answers into the slices that declare their read models, so they render like any other query.
/// </summary>
/// <remarks>
/// A query a performer answers is an opaque read (decision 0097): the code decides what it returns, so the executable model leaves it out of
/// <see cref="SemanticSlice.Queries"/> and carries only its shape - the read model, cardinality, delivery, caller-supplied arguments and who may call it - on the
/// <see cref="SemanticImplementationRole.QueryPerformer"/> requirement. The renderer reads that shape as a keyed query so the route, the parameters, the
/// authorization and the read model are the ones the model states; what the query answers is the performer's, so the generated method is a stub that names the
/// performer it waits for. A read model such a query returns needs no projection or reducer: the performer is what answers it.
/// </remarks>
internal static class SemanticPerformedQueries
{
    internal static Lifted Lift(ArtifactRenderRequest request)
    {
        var application = request.Model.Application;
        var requirements = request.ImplementationRequirements.IsDefault ? [] :
            request.ImplementationRequirements.Where(requirement => requirement is { Role: SemanticImplementationRole.QueryPerformer, QueryShape.ReadModel: not null }).ToArray();
        if (requirements.Length == 0)
        {
            return new(application, new HashSet<SemanticId>(), new Dictionary<SemanticId, string>());
        }

        var lifted = new Dictionary<SemanticId, List<SemanticKeyedQuery>>();
        var rejections = new Dictionary<SemanticId, string>();
        var performed = new HashSet<SemanticId>();
        foreach (var requirement in requirements)
        {
            var shape = requirement.QueryShape!;
            var readModel = shape.ReadModel!.Value;
            if (Rejection(application, requirement, shape) is { } rejection)
            {
                rejections.TryAdd(readModel, $"Performer query '{requirement.Owner.Name}' cannot render: {rejection}");
                continue;
            }

            var id = requirement.Source.SemanticId;
            var keys = shape.Arguments.Where(argument => argument.Kind == SemanticPerformedQueryArgumentKind.Key).ToArray();
            var filters = shape.Arguments.Where(argument => argument.Kind == SemanticPerformedQueryArgumentKind.Filter)
                .Select(argument => new SemanticQueryFilter(id, argument.Name, argument.Type! with { IsOptional = argument.IsOptional }, default)).ToImmutableArray();
            var query = new SemanticKeyedQuery(
                id,
                requirement.Owner.Name,
                keys.Length == 0 ? null : new SemanticReadModelQueryArgument(id, keys[0].Name, keys[0].Type! with { IsOptional = keys[0].IsOptional }),
                readModel,
                null,
                shape.Cardinality,
                shape.Delivery)
            {
                Authorization = shape.Authorization,
                Filters = filters
            };
            if (!lifted.TryGetValue(readModel, out var queries))
            {
                lifted[readModel] = queries = [];
            }

            queries.Add(query);
            performed.Add(id);
        }

        if (lifted.Count == 0)
        {
            return new(application, performed, rejections);
        }

        return new(
            application with { Modules = [.. application.Modules.Select(module => module with { Features = [.. module.Features.Select(feature => Lift(feature, lifted))] })] },
            performed,
            rejections);
    }

    static SemanticFeature Lift(SemanticFeature feature, Dictionary<SemanticId, List<SemanticKeyedQuery>> lifted) =>
        feature with
        {
            Features = [.. feature.Features.Select(nested => Lift(nested, lifted))],
            Slices = [.. feature.Slices.Select(slice => slice.ReadModels.Where(readModel => lifted.ContainsKey(readModel.Id)).ToArray() is { Length: > 0 } models
                ? slice with { Queries = [.. slice.Queries, .. models.SelectMany(model => lifted[model.Id])] }
                : slice)]
        };

    static string? Rejection(SemanticApplication application, SemanticImplementationRequirement requirement, SemanticPerformedQueryShape shape)
    {
        if (shape.Arguments.Any(argument => argument.Type is null))
        {
            return "an argument type does not resolve.";
        }

        if (shape.Arguments.Count(argument => argument.Kind == SemanticPerformedQueryArgumentKind.Key) > 1)
        {
            return "its composite 'by' key is not one argument.";
        }

        if (shape.Delivery == SemanticQueryDelivery.Live && shape.Cardinality != SemanticQueryCardinality.Many)
        {
            return "only a collection can be observed.";
        }

        var optionalSeen = false;
        foreach (var argument in shape.Arguments)
        {
            if (optionalSeen && !argument.IsOptional)
            {
                return $"required argument '{argument.Name}' follows an optional one.";
            }

            optionalSeen |= argument.IsOptional;
        }

        if (shape.Authorization is { } authorization && SemanticCratisAdmission.OpaquePolicies(authorization, application.Policies).Any())
        {
            return "its authorization names a policy implemented by code.";
        }

        return null;
    }

    /// <summary>The application the renderer sees, with every liftable performed query added to the slice of its read model.</summary>
    /// <param name="Application">The application.</param>
    /// <param name="Performed">The identities of the lifted queries.</param>
    /// <param name="Rejections">Why a performed query cannot render, by the identity of the read model it returns.</param>
    internal sealed record Lifted(SemanticApplication Application, IReadOnlySet<SemanticId> Performed, IReadOnlyDictionary<SemanticId, string> Rejections);
}
