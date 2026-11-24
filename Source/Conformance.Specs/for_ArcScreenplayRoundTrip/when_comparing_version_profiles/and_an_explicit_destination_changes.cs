// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_an_explicit_destination_changes : Specification
{
    [Fact]
    void should_not_fill_in_a_missing_destination_already_declared_at_the_common_profile()
    {
        var legacy = comparison_profile_models.Legacy;
        var profile = round_trip_model_profile.Common;
        var higher = ExecutableSemanticModel.Create(profile.Language, profile.Semantic, comparison_profile_models.Calendar(legacy).Application);

        legacy.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        higher.SemanticVersion.ShouldEqual(SemanticVersion.V48);
        Assert.All(comparison_profile_models.Commands(higher), command => Assert.Null(command.Destination));
        var comparison = round_trip_model_comparison.Compare(legacy, higher);
        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Contains(comparison.Differences, difference => difference.Path.EndsWith("/destination", StringComparison.Ordinal));
    }

    [Fact]
    void should_preserve_an_explicit_destination()
    {
        var model = comparison_profile_models.Explicit(comparison_profile_models.Legacy);
        var converted = round_trip_model_profile.Convert(model);

        comparison_profile_models.Commands(converted).Single().Destination.ShouldEqual(comparison_profile_models.Commands(model).Single().Destination);
    }
}
#endif
