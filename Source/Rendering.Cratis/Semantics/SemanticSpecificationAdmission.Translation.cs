// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks given-only Translate specifications that assert projected read-model state.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    internal static bool CanRenderGivenOnlyTranslationProjection(SemanticApplicationContext context, SemanticSlice slice, SemanticSpecification specification) =>
        slice.Kind == SemanticSliceKind.Translate && IsGivenOnlyProjection(specification) &&
        specification.GivenEvents.All(fact => IsLocalProjectionGiven(context, slice, fact)) &&
        SemanticProjectionFactSpecification.TryLower(context, specification, out var lowered) && CanRenderProjectedFacts(context, lowered);

    static bool IsLocalProjectionGiven(SemanticApplicationContext context, SemanticSlice slice, SemanticSpecificationEvent fact) =>
        fact.Route is null && fact.EventSource is { } source && TranslationSourceMatches(context, source) &&
        slice.Events.Any(contract => contract.Id == fact.EventContract && contract.Origin is null && contract.Visibility == SemanticEventVisibility.Private);

    static bool IsGivenOnlyProjection(SemanticSpecification specification) =>
        specification is
        {
            GivenEvents.IsEmpty: false,
            When: null, WhenAppended: null, WhenRedelivered: null, WhenCapture: null, WhenTrigger: null, WhenClock: null,
            GivenReadModels.IsEmpty: true, GivenCaptures.IsEmpty: true, GivenGenerated.IsEmpty: true,
            GivenCaller: null, GivenClock: null,
            ThenDenied: false, ThenReturns: null, ThenErrors.IsEmpty: true,
            ThenEvents.IsEmpty: true, ThenNoEvents: false, ThenEventsInAnyOrder: false,
            ThenInvocations.IsEmpty: true, ThenQueries.IsEmpty: true
        } && specification.ThenReadModels.Length + specification.ThenAbsentReadModels.Length == 1;
}
