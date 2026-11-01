// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisBootstrapLocations
{
    internal static IEnumerable<SemanticIdentityAssignment> Resolve(SemanticApplicationContext context, ArtifactRenderRequest request)
    {
        var catalog = request.IdentityCatalog;
        if (catalog is null) return [];
        return Addresses(context, catalog.Application, request).Select(owner => Resolve(catalog, owner.Id, owner.Address));
    }

    static SemanticIdentityAssignment Resolve(SemanticIdentityCatalog catalog, SemanticId identity, SemanticAddress address)
    {
        var assignment = catalog.ResolveSemanticAssignment(address);
        if (assignment.Id != identity) throw new InvalidArtifactRenderContract("A native catalog address does not resolve to its selected semantic owner.");
        return assignment;
    }

    static IEnumerable<(SemanticId Id, SemanticAddress Address)> Addresses(SemanticApplicationContext context, ApplicationIdentity application, ArtifactRenderRequest request) =>
        context.Application.Concepts.Select(concept => (concept.Id, SemanticAddress.ForConcept(application, concept.Name)))
            .Concat(context.Application.Types.Select(type => (type.Id, SemanticAddress.ForCompositeType(application, type.Name))))
            .Concat(context.SelectedSlices().SelectMany(located => InSlice(located, application, request)));

    static IEnumerable<(SemanticId Id, SemanticAddress Address)> InSlice(LocatedSemanticSlice located, ApplicationIdentity application, ArtifactRenderRequest request)
    {
        var slice = located.Slice;
        var address = SemanticAddress.ForSlice(application, located.Module.Name, [.. located.FeaturePath.Select(feature => feature.Name)], slice.Name);
        return slice.Commands.Select(command => (command.Id, SemanticAddress.ForCommand(address, command.Name)))
            .Concat(slice.Events.Select(@event => (@event.Id, SemanticAddress.ForEventContract(address, @event.Name))))
            .Concat(slice.ReadModels.Select(model => (model.Id, SemanticAddress.ForReadModel(address, model.Name))))
            .Concat(slice.Projections.Select(projection => (projection.Id, SemanticAddress.ForProjection(address, projection.Name))))
            .Concat(slice.Queries.Select(query => (query.Id, QueryAddress(query, address, request))))
            .Concat(slice.Reactions.Select(reaction => (reaction.Id, SemanticAddress.ForReaction(address, reaction.Name))))
            .Concat(slice.Captures.Select(capture => (capture.Id, SemanticAddress.ForCapture(address, capture.Name))))
            .Concat(slice.Specifications.Select(specification => (specification.Id, SemanticAddress.ForSpecification(address, specification.Name))));
    }

    static SemanticAddress QueryAddress(SemanticKeyedQuery query, SemanticAddress slice, ArtifactRenderRequest request) =>
        request.ImplementationRequirements.FirstOrDefault(requirement => requirement.Source.SemanticId == query.Id)?.Owner ?? SemanticAddress.ForQuery(slice, query.Name);
}
