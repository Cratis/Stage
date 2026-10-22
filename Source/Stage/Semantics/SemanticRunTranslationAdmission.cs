// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

internal static class SemanticRunTranslationAdmission
{
    internal static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        var slices = SemanticVersionFeatures.Slices(plan.Model.Application).ToArray();
        var features = slices.SelectMany(SemanticVersionFeatures.TranslationFeaturesInSlice).ToArray();
        var ownSlice = slices.FirstOrDefault(slice => slice.Specifications.Any(candidate => candidate.Id == specification.Id));
        var feature = ownSlice?.Kind == SemanticSliceKind.Translate
            ? features.First(candidate => candidate.Artifact == ownSlice.Id && candidate.Kind == "slice") : null;
        var explicitEvents = specification.GivenEvents.Concat(specification.ThenEvents).Select(occurrence => occurrence.EventContract)
            .Concat(specification.WhenAppended is { } appended ? [appended.EventContract] : []).ToHashSet();
        feature ??= features.FirstOrDefault(candidate => candidate.Kind == "event" && explicitEvents.Contains(candidate.Artifact));
        if (feature is not null) return new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");

        var (reachable, _, blocker) = SemanticRunReactionAdmission.Reachable(plan, specification, [], includeRejected: true);
        if (blocker is not null) return blocker;
        foreach (var slice in slices)
        {
            var projections = slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event &&
                Observes(projection, reachable)).Select(projection => projection.Id);
            var reducers = slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event &&
                reducer.Transitions.Any(transition => reachable.Contains(transition.EventContract))).Select(reducer => reducer.ReadModel);
            var captures = (slice.Captures.IsDefault ? [] : slice.Captures).Where(capture =>
                capture.EventsSource is { } source && source.Events.Any(reachable.Contains)).Select(capture => capture.Id);
            var artifacts = projections.Concat(reducers).Concat(captures).ToHashSet();
            feature = features.FirstOrDefault(candidate => candidate.Kind != "event" && (candidate.Kind != "capture" || candidate.Code == SemanticVersionFeatures.ForeignEvents) && artifacts.Contains(candidate.Artifact));
            if (feature is not null) return new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
            if (slice.Kind == SemanticSliceKind.Translate &&
                (slice.Reactions.Any(reaction => reaction.Triggers.Any(trigger => trigger.Kind == SemanticReactionTriggerKind.Event &&
                    (reachable.Contains(trigger.Source) || (!trigger.Source.IsSet && reachable.Count > 0)))) ||
                    slice.Projections.Any(projection => Observes(projection, reachable)) ||
                    slice.Reducers.Any(reducer => reducer.Transitions.Any(transition => reachable.Contains(transition.EventContract)))))
            {
                feature = features.First(candidate => candidate.Artifact == slice.Id && candidate.Kind == "slice");
                return new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
            }
        }
        feature = features.FirstOrDefault(candidate => candidate.Kind == "event" && reachable.Contains(candidate.Artifact));
        return feature is null ? null : new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
    }

    static bool Observes(SemanticProjection projection, HashSet<SemanticId> reachable) =>
        (projection.Scope?.Every?.SubscribesToAllEvents == true && reachable.Count > 0) ||
        ProjectionReferencedEventNamesAreUnique.Contracts(projection).Any(reachable.Contains);
}
