// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class round_trip_model_comparison
{
    public static SemanticModelComparison Compare(ExecutableSemanticModel expected, ExecutableSemanticModel actual)
    {
        var profile = round_trip_model_profile.Common;
        var provenance = $"Arc round-trip versions: expected ({expected.LanguageVersion}, {expected.SemanticVersion}), actual ({actual.LanguageVersion}, {actual.SemanticVersion}), common ({profile.Language}, {profile.Semantic})";
        Console.WriteLine(provenance);
        try
        {
            var expectedModel = round_trip_model_profile.Convert(expected);
            var actualModel = round_trip_model_profile.Convert(actual);
            return SemanticModelEquivalence.Compare(Normalize(expectedModel), Normalize(actualModel));
        }
        catch (InvalidSemanticContract exception)
        {
            // Let the existing fixture-error guard fail the run; conversion is never an accepted loss or a null observation.
            throw new InvalidSemanticContract($"{provenance}: {exception.Message}");
        }
    }

    static NormalizedSemanticModel Normalize(ExecutableSemanticModel model) => round_trip_invocation_mappings.Normalize(SemanticModelNormalizer.Normalize(model));
}
#endif
