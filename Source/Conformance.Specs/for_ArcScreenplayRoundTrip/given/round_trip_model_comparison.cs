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
        var covered = round_trip_model_profile.Covers(expected) && round_trip_model_profile.Covers(actual);
        var normalization = DescribeNormalization(shared, covered);
        var provenance = $"Arc round-trip versions: expected ({expected.LanguageVersion}, {expected.SemanticVersion}), actual ({actual.LanguageVersion}, {actual.SemanticVersion}), {normalization}";
        Console.WriteLine(provenance);
        try
        {
            round_trip_model_profile.EnsureSupported(expected);
            round_trip_model_profile.EnsureSupported(actual);
            if (shared && covered) return Compare(round_trip_shared_version.Normalize(expected), round_trip_shared_version.Normalize(actual));
            return CompareValidated(expected, actual, shared, covered);
        }
        catch (InvalidSemanticContract exception)
        {
            // Invalid sources, unsupported pairs and non-meaning conversion failures still reach the fixture-error guard.
            throw new InvalidSemanticContract($"{provenance}: {exception.Message}");
        }
    }

    static string DescribeNormalization(bool shared, bool covered)
    {
        if (shared) return "shared, no promotion";
        return covered ? $"common {round_trip_model_profile.Common}" : "native versions, no promotion";
    }

    static SemanticModelComparison CompareValidated(ExecutableSemanticModel expected, ExecutableSemanticModel actual, bool shared, bool covered)
    {
        var expectedModel = round_trip_model_profile.ValidateNative(expected);
        var actualModel = round_trip_model_profile.ValidateNative(actual);
        round_trip_meaning_gates.meaning_change change = null!;
        var meaningLoss = !shared && (round_trip_meaning_gates.TryFind(expectedModel, out change) || round_trip_meaning_gates.TryFind(actualModel, out change));
        var payload = Payload(expectedModel, actualModel, covered, meaningLoss);
        return meaningLoss ? WithMeaningLoss(payload, expectedModel, actualModel, change) : payload;
    }

    static SemanticModelComparison Payload(ExecutableSemanticModel expected, ExecutableSemanticModel actual, bool covered, bool meaningLoss)
    {
        // Native comparison retains version headers and ownership; it neither fabricates a promotion nor claims equivalence.
        if (!covered) return Compare(SemanticModelNormalizer.Normalize(expected), SemanticModelNormalizer.Normalize(actual));
        if (meaningLoss) return Compare(round_trip_model_profile.NormalizePayload(expected), round_trip_model_profile.NormalizePayload(actual));
        return Compare(SemanticModelNormalizer.Normalize(round_trip_model_profile.Convert(expected)), SemanticModelNormalizer.Normalize(round_trip_model_profile.Convert(actual)));
    }

    static SemanticModelComparison WithMeaningLoss(SemanticModelComparison payload, ExecutableSemanticModel expected, ExecutableSemanticModel actual, round_trip_meaning_gates.meaning_change change)
    {
        // The mandatory extra difference forbids semantic equivalence even when payloads otherwise agree.
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
