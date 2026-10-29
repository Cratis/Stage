// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Keeps direct-handler specifications from bypassing effects that require an event-store scenario.
/// </summary>
internal static partial class SemanticTranslationSpecificationAdmission
{
    static string? EffectRejection(SemanticApplicationContext context, SemanticSlice slice, SemanticId input, IEnumerable<SemanticId> outputs)
    {
        var sources = EffectSources(slice).ToArray();
        if (sources.Count(source => source == input) != 1)
            return "direct handler scenarios do not settle event fan-out across captures, publications, or reactions.";
        if (outputs.Any(output => sources.Contains(output)))
            return "direct handler scenarios do not settle cascades through other captures, publications, or reactions.";
        if (outputs.Append(input).Any(contract => SemanticSpecificationAdmission.IsReactionConstraintTarget(context, contract)))
            return "direct handler scenarios cannot enforce append-time constraints.";
        return null;
    }

    // A specification runs only its own slice's captures, publications and reactions (Screenplay decision 0115), so another
    // slice observing the same event neither fans the run out nor continues its cascade.
    static IEnumerable<SemanticId> EffectSources(SemanticSlice slice) =>
        slice.Captures.Where(capture => capture.EventsSource is not null).SelectMany(capture => capture.EventsSource!.Events)
            .Concat(slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event)
                .SelectMany(projection => projection.Scope?.From.Select(source => source.EventContract) ?? []))
            .Concat((slice.Reactions.IsDefault ? [] : slice.Reactions).SelectMany(reaction => reaction.Triggers.Select(trigger => trigger.Source)));
}
