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
        var unsupportedSlices = slices.Where(slice => !SemanticTranslationSupport.Slice(slice, plan.Events) ||
            !slice.Reactions.IsDefaultOrEmpty || slice.Projections.Any(projection => projection.Target != SemanticProjectionTargetKind.Event) ||
            slice.Reducers.Any(reducer => reducer.Target != SemanticProjectionTargetKind.Event)).Select(slice => slice.Id).ToHashSet();
        var features = slices.SelectMany(SemanticVersionFeatures.TranslationFeaturesInSlice).Where(candidate => candidate.Kind switch
        {
            "slice" => unsupportedSlices.Contains(candidate.Artifact),
            "event" => !SemanticTranslationSupport.Event(plan.Events[candidate.Artifact], slices, plan.Events),
            "capture" => !SemanticTranslationSupport.Capture(plan.Captures[candidate.Artifact], plan.Events),
            "projection" => !SemanticTranslationSupport.Projection(plan.Projections[candidate.Artifact], plan.Events),
            _ => true
        }).ToArray();
        var ownSlice = slices.FirstOrDefault(slice => slice.Specifications.Any(candidate => candidate.Id == specification.Id));
        var feature = ownSlice?.Kind == SemanticSliceKind.Translate && !SemanticTranslationSupport.Slice(ownSlice, plan.Events)
            ? features.FirstOrDefault(candidate => candidate.Artifact == ownSlice.Id && candidate.Kind == "slice") : null;
        var explicitEvents = specification.GivenEvents.Concat(specification.ThenEvents).Select(occurrence => occurrence.EventContract)
            .Concat(specification.WhenAppended is { } appended ? [appended.EventContract] : []).ToHashSet();
        feature ??= features.FirstOrDefault(candidate => candidate.Kind == "event" && explicitEvents.Contains(candidate.Artifact));
        if (feature is not null) return new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");

        var (reachable, _, blocker) = SemanticRunReactionAdmission.Reachable(plan, specification, [], includeRejected: true);
        if (blocker is not null) return blocker;
        int previousCount;
        do
        {
            previousCount = reachable.Count;
            foreach (var source in reachable.ToArray())
            {
                var delivered = new SemanticSpecification(source, "Translation reachability", [], [], null, [], [], [], []) { WhenAppended = new(source, []) };
                var (effects, _, failure) = SemanticRunReactionAdmission.Reachable(plan, delivered, [], includeRejected: true);
                if (failure is not null) return failure;
                reachable.UnionWith(effects);
            }
            reachable.UnionWith(plan.Captures.Values.Where(capture => capture.EventsSource is { } source && source.Events.Any(reachable.Contains))
                .SelectMany(capture => capture.Appends).Select(append => append.EventContract));
            reachable.UnionWith(plan.Projections.Values.Where(projection => projection.Target == SemanticProjectionTargetKind.Event &&
                ((projection.Scope?.Every?.SubscribesToAllEvents == true && reachable.Count > 0) || ProjectionReferencedEventNamesAreUnique.Contracts(projection).Any(reachable.Contains)))
                .Select(projection => projection.ReadModel));
        }
        while (previousCount != reachable.Count);
        feature = features.FirstOrDefault(candidate => candidate.Kind == "event" && reachable.Contains(candidate.Artifact));
        if (feature is not null) return new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
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
                    slice.Projections.Any(projection => projection.Target != SemanticProjectionTargetKind.Event && Observes(projection, reachable)) ||
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
