// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Refuses historical event shapes before a typed context can bind them to a current generated record.
/// </summary>
internal static class SemanticEventLineageAdmission
{
    internal static ImmutableArray<ArtifactRenderDiagnostic> Verify(ArtifactRenderRequest request)
    {
        var events = request.Model.Application.Modules.SelectMany(module => module.Features)
            .SelectMany(AllSlices).SelectMany(slice => slice.Events).Where(@event => !@event.PriorRevisions.IsEmpty).ToArray();
        var errors = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (request.TypedContextDescriptors.IsDefault)
        {
            return errors.ToImmutable();
        }

        foreach (var descriptor in request.TypedContextDescriptors.Where(descriptor => descriptor?.Members.IsDefault == false))
        {
            foreach (var member in descriptor.Members.Where(member => member is not null))
            {
                foreach (var @event in events)
                {
                    var revision = @event.PriorRevisions.FirstOrDefault(prior =>
                        (member.Source?.SemanticId == @event.Id && member.Source.EventRevision == prior.Revision) ||
                        prior.Properties.Any(property => property.Id == member.Source?.SemanticId) ||
                        (member.Type?.Shape == @event.Id && !member.Type.Properties.IsDefault &&
                         member.Type.Properties.Any(property => prior.Properties.Any(historical => historical.Id == property.Id))));
                    if (revision is not null)
                    {
                        errors.Add(new(
                            "STAGE-ESM-025",
                            ArtifactRenderDiagnosticSeverity.Error,
                            $"Typed context '{descriptor.RequirementId}' member '{member.Name}' references historical event '{@event.Name}' revision {revision.Revision.Value}; only current revision {@event.Revision.Value} can render.",
                            @event.Id));
                    }
                }
            }
        }

        return errors.ToImmutable();
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
