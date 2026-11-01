// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisSelectedSemanticOwners
{
    internal static IEnumerable<(SemanticId Id, ArtifactRenderConstructKind Kind)> Enumerate(SemanticApplicationContext context)
    {
        var application = context.Application;
        return application.Concepts.Select(concept => (concept.Id, ArtifactRenderConstructKind.Concept))
            .Concat(application.Types.Select(type => (type.Id, ArtifactRenderConstructKind.CompositeType)))
            .Concat(context.SelectedSlices().SelectMany(located => InSlice(located.Slice)));
    }

    static IEnumerable<(SemanticId Id, ArtifactRenderConstructKind Kind)> InSlice(SemanticSlice slice) =>
        slice.Commands.Select(command => (command.Id, ArtifactRenderConstructKind.Command))
            .Concat(slice.Events.Select(@event => (@event.Id, ArtifactRenderConstructKind.Event)))
            .Concat(slice.ReadModels.Select(model => (model.Id, ArtifactRenderConstructKind.ReadModel)))
            .Concat(slice.Projections.Select(projection => (projection.Id, ArtifactRenderConstructKind.Projection)))
            .Concat(slice.Queries.Select(query => (query.Id, ArtifactRenderConstructKind.Query)))
            .Concat(slice.Reactions.Select(reaction => (reaction.Id, ArtifactRenderConstructKind.Reaction)))
            .Concat(slice.Captures.Select(capture => (capture.Id, ArtifactRenderConstructKind.Capture)))
            .Concat(slice.Specifications.Select(specification => (specification.Id, ArtifactRenderConstructKind.Specification)));
}
