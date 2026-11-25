// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_the_versions_are_shared : Specification
{
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    void should_compare_legacy_meaning_without_attempting_promotion(int version)
    {
        var model = version == 1 ? comparison_profile_models.Legacy : meaning_gate_models.Response("Int", "1");

        Assert.Throws<InvalidSemanticContract>(() => round_trip_model_profile.Convert(model));
        round_trip_model_comparison.Compare(model, model).IsEquivalent.ShouldBeTrue();
    }

    [Fact]
    void should_keep_payload_differences_visible_at_the_shared_version()
    {
        var model = meaning_gate_models.Response("Int", "1");
        var application = comparison_profile_models.Commands(model.Application, command => command with { Name = "RenamedDeposit" });
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        var comparison = round_trip_model_comparison.Compare(model, changed);

        comparison.IsEquivalent.ShouldBeFalse();
        Assert.DoesNotContain(comparison.Differences, difference => difference.Path.StartsWith("/version-meaning-unproven", StringComparison.Ordinal));
    }

    [Fact]
    void should_compare_without_promotion_when_the_shared_introduction_requires_unavailable_content()
    {
        var model = comparison_profile_models.Legacy;
        var historical = comparison_profile_models.Unchecked(model, LanguageVersion.V2, SemanticVersion.V2, model.Application);

        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(historical.LanguageVersion, historical.SemanticVersion, historical.Application));
        round_trip_model_comparison.Compare(historical, historical).IsEquivalent.ShouldBeTrue();
    }

    [Fact]
    void should_not_hide_a_genuinely_invalid_shared_contract()
    {
        var model = comparison_profile_models.Migratable;
        var application = model.Application with { Modules = [model.Application.Modules[0], model.Application.Modules[0]] };
        var invalid = comparison_profile_models.Unchecked(model, model.LanguageVersion, model.SemanticVersion, application);

        Assert.Throws<InvalidSemanticContract>(() => round_trip_model_comparison.Compare(invalid, invalid));
    }
}
#endif
