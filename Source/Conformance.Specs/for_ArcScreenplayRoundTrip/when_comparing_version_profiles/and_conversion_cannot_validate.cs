// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_conversion_cannot_validate : Specification
{
    [Fact]
    void should_convert_a_graph_free_model_to_the_common_profile()
    {
        var converted = round_trip_model_profile.Convert(comparison_profile_models.Migratable);

        converted.LanguageVersion.ShouldEqual(LanguageVersion.V48);
        converted.SemanticVersion.ShouldEqual(SemanticVersion.V48);
        Assert.Empty(converted.Application.Graphs);
        Assert.Null(converted.Application.Agents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_convert_v38_and_graph_free_models_without_concealing_the_missing_graph(bool reverse)
    {
        var legacy = comparison_profile_models.Migratable;
        var graph = comparison_profile_models.Golden(38);
        var converted = round_trip_model_profile.Convert(graph);
        var comparison = reverse ? round_trip_model_comparison.Compare(graph, legacy) : round_trip_model_comparison.Compare(legacy, graph);

        converted.SemanticVersion.ShouldEqual(SemanticVersion.V48);
        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Contains(comparison.Differences, difference => difference.Path.StartsWith("/application/graphs", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_fail_instead_of_returning_an_observation_that_could_escape_fixture_errors(bool reverse)
    {
        var legacy = comparison_profile_models.Migratable;
        var invalidApplication = legacy.Application with { Modules = [legacy.Application.Modules[0], legacy.Application.Modules[0]] };
        var invalid = comparison_profile_models.Unchecked(legacy, legacy.LanguageVersion, legacy.SemanticVersion, invalidApplication);
        var expected = Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(legacy.LanguageVersion, legacy.SemanticVersion, invalidApplication));

        var exception = Assert.Throws<InvalidSemanticContract>(() => reverse
            ? round_trip_model_comparison.Compare(invalid, legacy)
            : round_trip_model_comparison.Compare(legacy, invalid));

        Assert.Contains(legacy.SemanticVersion.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    void should_reach_the_existing_fixture_error_guard_from_the_real_harness()
    {
        var fixture = round_trip_fixtures.Inline(round_trip_fixtures.RoutedCommand) with { Name = "UnconvertibleProfile" };
        var model = fixture.Model;
        var invalidApplication = model.Application with { Modules = [model.Application.Modules[0], model.Application.Modules[0]] };
        var invalid = comparison_profile_models.Unchecked(model, model.LanguageVersion, model.SemanticVersion, invalidApplication);
        var expected = Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, invalidApplication));
        var fixtureErrors = new List<string>();
        try
        {
            arc_round_trip.Run(fixture with { Model = invalid });
        }
        catch (Exception exception)
        {
            fixtureErrors.Add($"{fixture.Name}: {exception}");
        }

        Assert.Single(fixtureErrors);
        Assert.Contains(expected.Message, fixtureErrors[0], StringComparison.Ordinal);
    }

    [Fact]
    void should_fail_an_above_profile_fixture_before_attempting_render_or_recovery()
    {
        var fixture = new round_trip_fixture("AboveProfile", comparison_profile_models.Golden(49), null!);
        var exception = Assert.Throws<InvalidSemanticContract>(() => arc_round_trip.Run(fixture));

        Assert.Contains("cannot downgrade", exception.Message, StringComparison.Ordinal);
        Assert.Contains("49.0", exception.Message, StringComparison.Ordinal);
        Assert.Contains("48.0", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_a_valid_source_above_the_common_profile_instead_of_downgrading_it(bool reverse)
    {
        var legacy = comparison_profile_models.Migratable;
        var newer = comparison_profile_models.Golden(49);
        var exception = Assert.Throws<InvalidSemanticContract>(() => reverse
            ? round_trip_model_comparison.Compare(newer, legacy)
            : round_trip_model_comparison.Compare(legacy, newer));

        Assert.Contains("49.0", exception.Message, StringComparison.Ordinal);
        Assert.Contains("48.0", exception.Message, StringComparison.Ordinal);
        Assert.Contains("cannot downgrade", exception.Message, StringComparison.Ordinal);
    }
}
#endif
