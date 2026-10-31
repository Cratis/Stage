// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits direct inbox captures and outbox publications without dropping other modeled outcomes.
/// </summary>
internal static partial class SemanticTranslationSpecificationAdmission
{
    internal static string? CannotRender(SemanticApplicationContext context, SemanticSlice slice, SemanticSpecification specification)
    {
        if (specification.WhenAppended is not { } append || specification.When is not null || specification.WhenRedelivered is not null)
        {
            return GivenOnlyProjectionRejection(context, slice, specification);
        }
        if (UnsupportedTranslationFixtures(specification))
        {
            return "direct Translate scenarios support ordered event outcomes, not caller, generated, capture, clock, read, return, denial, refusal or unordered outcomes.";
        }
        if (append.Route is not null || specification.GivenEvents.Any(given => given.Route is not null) || specification.ThenEvents.Any(expected => expected.Route is not null))
        {
            return "routed Translate fixtures are not supported.";
        }
        var captures = slice.Captures.Where(capture => capture.EventsSource?.Events.Contains(append.EventContract) == true).ToArray();
        var publications = slice.Projections.Where(projection => projection.Scope?.From.Any(from => from.EventContract == append.EventContract) == true).ToArray();
        if (captures.Length + publications.Length != 1)
        {
            return "the append must select exactly one inbox capture or outbox publication.";
        }
        if (!HistoryMatches(context, specification, captures.FirstOrDefault(), publications.FirstOrDefault()))
        {
            return "given history must contain the capture's own foreign events, or explicitly sourced private facts that do not trigger the selected translation.";
        }
        if (publications.Length != 0 && append.EventSource is null)
        {
            return "an outbox publication input requires an explicit event source.";
        }
        if (!ValuesMatch(context, specification))
        {
            return "Translate fixtures require complete, comparable event values and lossless event sources.";
        }
        var targets = captures.Length != 0 ? captures[0].Appends.Select(append => append.EventContract) : [publications[0].ReadModel];
        if (EffectRejection(context, slice, append.EventContract, targets) is { } effects) return effects;
        if (specification.ThenEvents.Any(expected => !targets.Contains(expected.EventContract)))
        {
            return "the expected outcomes require a cascade beyond the selected translation; no outcomes can be silently omitted.";
        }
        return null;
    }

    internal static SemanticCapture? Capture(SemanticSlice slice, SemanticSpecificationAppend append) =>
        slice.Captures.SingleOrDefault(capture => capture.EventsSource?.Events.Contains(append.EventContract) == true);

    /// <summary>Lists the contracts the selected capture or publication resolves through the event store's type registry.</summary>
    /// <param name="slice">The declaring slice.</param>
    /// <param name="append">The appended input.</param>
    /// <returns>The source and target contracts of the selected translation, never contracts outside the slice scope.</returns>
    internal static IEnumerable<SemanticId> SelectedContracts(SemanticSlice slice, SemanticSpecificationAppend append)
    {
        if (Capture(slice, append) is { } capture) return capture.EventsSource!.Events.Concat(capture.Appends.Select(target => target.EventContract));
        return Publication(slice, append) is { } publication ? [publication.ReadModel] : [];
    }

    internal static SemanticProjection? Publication(SemanticSlice slice, SemanticSpecificationAppend append) =>
        slice.Projections.SingleOrDefault(projection => projection.Scope?.From.Any(from => from.EventContract == append.EventContract) == true);

    static string? GivenOnlyProjectionRejection(SemanticApplicationContext context, SemanticSlice slice, SemanticSpecification specification) =>
        SemanticSpecificationAdmission.CanRenderGivenOnlyTranslationProjection(context, slice, specification) ? null :
            "a Translate scenario requires one direct 'when append' input or one read-model expectation over sourced private local Given events; general cascades and redelivery are not supported.";

    static bool UnsupportedTranslationFixtures(SemanticSpecification specification) =>
        !specification.ThenInvocations.IsEmpty || UnsupportedFixtures(specification);

    static bool UnsupportedFixtures(SemanticSpecification specification) =>
        specification.GivenCaller is not null || !specification.GivenGenerated.IsDefaultOrEmpty || !specification.GivenCaptures.IsDefaultOrEmpty ||
        !specification.GivenReadModels.IsEmpty || !specification.ThenErrors.IsEmpty || !specification.ThenReadModels.IsEmpty ||
        !specification.ThenQueries.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty || specification.ThenReturns is not null || specification.ThenDenied ||
        specification.GivenClock is not null || specification.WhenClock is not null || specification.WhenTrigger is not null || specification.WhenCapture is not null ||
        specification.ThenEventsInAnyOrder || (specification.ThenEvents.IsEmpty == !specification.ThenNoEvents);

    static bool HistoryMatches(SemanticApplicationContext context, SemanticSpecification specification, SemanticCapture? capture, SemanticProjection? publication)
    {
        var triggers = capture is not null ? capture.EventsSource!.Events.AsEnumerable() : publication!.Scope!.From.Select(from => from.EventContract);
        return specification.GivenEvents.All(given => (capture is not null && triggers.Contains(given.EventContract)) || (!triggers.Contains(given.EventContract) && given.EventSource is not null &&
            context.Events[given.EventContract].Visibility == SemanticEventVisibility.Private && context.Events[given.EventContract].Origin is null));
    }

    static bool ValuesMatch(SemanticApplicationContext context, SemanticSpecification specification) =>
        SemanticSpecificationAdmission.TranslationValuesMatch(context, specification.WhenAppended!.EventContract, specification.WhenAppended.Values) &&
        specification.GivenEvents.Concat(specification.ThenEvents).All(fact => SemanticSpecificationAdmission.TranslationValuesMatch(context, fact.EventContract, fact.Values)) &&
        specification.GivenEvents.Concat(specification.ThenEvents).Select(fact => fact.EventSource).Append(specification.WhenAppended.EventSource)
            .All(source => source is null || SemanticSpecificationAdmission.TranslationSourceMatches(context, source));
}
