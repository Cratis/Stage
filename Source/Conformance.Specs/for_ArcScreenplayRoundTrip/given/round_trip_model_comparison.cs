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
        var shared = expected.LanguageVersion == actual.LanguageVersion && expected.SemanticVersion == actual.SemanticVersion;
        var provenance = $"Arc round-trip versions: expected ({expected.LanguageVersion}, {expected.SemanticVersion}), actual ({actual.LanguageVersion}, {actual.SemanticVersion}), {(shared ? "shared, no promotion" : $"common {round_trip_model_profile.Common}")}";
        Console.WriteLine(provenance);
        try
        {
            round_trip_model_profile.EnsureCovered(expected);
            round_trip_model_profile.EnsureCovered(actual);
            if (shared) return Compare(round_trip_shared_version.Normalize(expected), round_trip_shared_version.Normalize(actual));
            return CompareDifferent(expected, actual);
        }
        catch (InvalidSemanticContract exception)
        {
            // Invalid sources, unsupported pairs and non-meaning conversion failures still reach the fixture-error guard.
            throw new InvalidSemanticContract($"{provenance}: {exception.Message}");
        }
    }

    static SemanticModelComparison CompareDifferent(ExecutableSemanticModel expected, ExecutableSemanticModel actual)
    {
        var expectedModel = ExecutableSemanticModel.Create(expected.LanguageVersion, expected.SemanticVersion, expected.Application);
        var actualModel = ExecutableSemanticModel.Create(actual.LanguageVersion, actual.SemanticVersion, actual.Application);
        if (round_trip_meaning_gates.TryFind(expectedModel, out var change) || round_trip_meaning_gates.TryFind(actualModel, out change))
        {
            return WithMeaningLoss(expectedModel, actualModel, change);
        }
        return Compare(SemanticModelNormalizer.Normalize(round_trip_model_profile.Convert(expectedModel)), SemanticModelNormalizer.Normalize(round_trip_model_profile.Convert(actualModel)));
    }

    static SemanticModelComparison WithMeaningLoss(ExecutableSemanticModel expected, ExecutableSemanticModel actual, round_trip_meaning_gates.meaning_change change)
    {
        // This shape-only projection retains existing payload losses; the mandatory extra difference forbids semantic equivalence.
        var payload = Compare(round_trip_model_profile.NormalizePayload(expected), round_trip_model_profile.NormalizePayload(actual));
        var loss = new SemanticDifference(
            change.DifferencePath,
            SemanticDifferenceKind.Changed,
            $"({expected.LanguageVersion}, {expected.SemanticVersion}); gate {change.Name} at {change.Path}",
            $"({actual.LanguageVersion}, {actual.SemanticVersion}); recovered version differs and no meaning-preserving promotion is proven for {change.Name}");
        return new([.. payload.Differences.Add(loss).OrderBy(difference => difference.Path, StringComparer.Ordinal)]);
    }

    static SemanticModelComparison Compare(NormalizedSemanticModel expected, NormalizedSemanticModel actual) =>
        SemanticModelEquivalence.Compare(round_trip_invocation_mappings.Normalize(expected), round_trip_invocation_mappings.Normalize(actual));
}
#endif
