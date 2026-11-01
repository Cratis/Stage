// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Rendering.Model;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents one selected command or reaction the declarative planner accounts for.
/// </summary>
/// <param name="Id">The construct identity.</param>
/// <param name="Kind">The construct kind.</param>
/// <param name="Name">The construct name.</param>
/// <param name="Address">The construct's semantic address.</param>
sealed record DeclarativeConstruct(SemanticId Id, ArtifactRenderConstructKind Kind, string Name, SemanticAddress Address)
{
    /// <summary>
    /// Gets the placeholder kind used when the kit renders nothing for the construct.
    /// </summary>
    internal ArtifactRenderPlaceholderKind Placeholder => Kind == ArtifactRenderConstructKind.Reaction
        ? ArtifactRenderPlaceholderKind.UnregisteredConsumer
        : ArtifactRenderPlaceholderKind.DenyEnclosingOperation;

    /// <summary>
    /// Selects the commands and reactions of the slices the request covers.
    /// </summary>
    /// <param name="request">The render request.</param>
    /// <returns>The selected constructs in declaration order.</returns>
    internal static IReadOnlyList<DeclarativeConstruct> Select(ArtifactRenderRequest request)
    {
        var application = request.IdentityCatalog?.Application ?? ApplicationIdentity.Create(request.Model.Application.Name);
        var constructs = new List<DeclarativeConstruct>();
        foreach (var located in RenderScopeSelection.Select(request))
        {
            var slice = SemanticAddress.ForSlice(application, located.Module.Name, [.. located.Features.Select(feature => feature.Name)], located.Slice.Name);
            constructs.AddRange(located.Slice.Commands.Select(command => new DeclarativeConstruct(command.Id, ArtifactRenderConstructKind.Command, command.Name, SemanticAddress.ForCommand(slice, command.Name))));
            constructs.AddRange(located.Slice.Reactions.Select(reaction => new DeclarativeConstruct(reaction.Id, ArtifactRenderConstructKind.Reaction, reaction.Name, SemanticAddress.ForReaction(slice, reaction.Name))));
        }

        return constructs;
    }
}
