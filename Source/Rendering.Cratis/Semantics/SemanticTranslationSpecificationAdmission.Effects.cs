// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Keeps direct-handler specifications from bypassing effects that require an event-store scenario.
/// </summary>
internal static partial class SemanticTranslationSpecificationAdmission
{
    static string? EffectRejection(SemanticApplicationContext context, SemanticId input, IEnumerable<SemanticId> outputs)
    {
        var sources = EffectSources(context).ToArray();
        if (sources.Count(source => source == input) != 1)
            return "direct handler scenarios do not settle event fan-out across captures, publications, or reactions.";
        if (outputs.Any(output => sources.Contains(output)))
            return "direct handler scenarios do not settle cascades through other captures, publications, or reactions.";
        if (outputs.Append(input).Any(contract => SemanticSpecificationAdmission.IsReactionConstraintTarget(context, contract)))
            return "direct handler scenarios cannot enforce append-time constraints.";
        return null;
    }

    static IEnumerable<SemanticId> EffectSources(SemanticApplicationContext context) => context.AllSlices.SelectMany(located =>
        located.Slice.Captures.Where(capture => capture.EventsSource is not null).SelectMany(capture => capture.EventsSource!.Events)
            .Concat(located.Slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event)
                .SelectMany(projection => projection.Scope?.From.Select(source => source.EventContract) ?? []))
            .Concat(located.Slice.Reactions.SelectMany(reaction => reaction.Triggers.Select(trigger => trigger.Source))));
}
