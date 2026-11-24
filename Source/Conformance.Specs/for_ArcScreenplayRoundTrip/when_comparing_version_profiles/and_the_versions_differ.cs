// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Conformance;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_the_versions_differ : Specification
{
    ExecutableSemanticModel _legacy;
    ExecutableSemanticModel _explicit;
    byte[] _original;
    SemanticModelComparison _comparison;

    void Establish()
    {
        _legacy = comparison_profile_models.Migratable;
        _explicit = comparison_profile_models.Calendar(_legacy);
        _original = SemanticModelSerializer.Serialize(_legacy);
    }

    void Because() => _comparison = round_trip_model_comparison.Compare(_legacy, _explicit);

    [Fact] void should_compare_semantically_safe_response_contracts() => _comparison.IsEquivalent.ShouldBeTrue();
    [Fact] void should_work_with_either_side_as_the_higher_version() => round_trip_model_comparison.Compare(_explicit, _legacy).IsEquivalent.ShouldBeTrue();
    [Fact]
    void should_leave_the_general_comparer_version_strict()
    {
        _legacy.LanguageVersion.ShouldEqual(LanguageVersion.V7);
        _legacy.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        _explicit.LanguageVersion.ShouldEqual(LanguageVersion.V37);
        _explicit.SemanticVersion.ShouldEqual(SemanticVersion.V37);

        var comparison = SemanticModelEquivalence.Compare(_legacy, _explicit);
        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Contains(comparison.Differences, difference => difference.Path == "/languageVersion");
        Assert.Contains(comparison.Differences, difference => difference.Path == "/semanticVersion");
    }
    [Fact] void should_leave_the_raw_model_unchanged() => Assert.Equal(_original, SemanticModelSerializer.Serialize(_legacy));

    [Fact]
    void should_refuse_historical_defaults_when_their_execution_meaning_is_unproven()
    {
        var legacy = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.LegacyV1.EsmBytes.AsSpan());
        var current = comparison_profile_models.Calendar(comparison_profile_models.Explicit(legacy));
        Assert.Throws<InvalidSemanticContract>(() => round_trip_model_profile.Convert(legacy));
        var comparison = round_trip_model_comparison.Compare(legacy, current);
        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Contains(comparison.Differences, difference => difference.Path.Contains("version-meaning-unproven[V1 occurrence identity/context]", StringComparison.Ordinal));
    }

    [Fact]
    void should_retain_both_version_fields_and_be_idempotent()
    {
        var converted = round_trip_model_profile.Convert(_legacy);
        var normalized = SemanticModelNormalizer.Normalize(converted);

        normalized.Root.Members["languageVersion"].Value.ShouldEqual("\"48.0\"");
        normalized.Root.Members["semanticVersion"].Value.ShouldEqual("\"48.0\"");
        Assert.Equal(SemanticModelSerializer.Serialize(converted), SemanticModelSerializer.Serialize(round_trip_model_profile.Convert(converted)));
    }
}
#endif
