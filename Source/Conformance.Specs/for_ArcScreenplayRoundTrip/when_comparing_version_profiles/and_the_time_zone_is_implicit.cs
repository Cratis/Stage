// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_the_time_zone_is_implicit : Specification
{
    ExecutableSemanticModel _legacy;
    ExecutableSemanticModel _utc;

    void Establish()
    {
        _legacy = comparison_profile_models.Migratable;
        _utc = comparison_profile_models.Calendar(_legacy);
    }

    [Fact] void should_equal_the_nonconfigurable_utc_default() => round_trip_model_comparison.Compare(_legacy, _utc).IsEquivalent.ShouldBeTrue();

    [Theory]
    [InlineData("Europe/Oslo", false)]
    [InlineData("UTC", true)]
    void should_not_erase_a_real_zone_or_configurability_change(string zone, bool configurable) =>
        round_trip_model_comparison.Compare(_legacy, comparison_profile_models.Calendar(_legacy, zone, configurable)).IsEquivalent.ShouldBeFalse();

    [Fact]
    void should_not_erase_context_members()
    {
        var changed = ExecutableSemanticModel.Create(_utc.LanguageVersion, _utc.SemanticVersion, _utc.Application with { ContextMembers = ["officeTimeZone"] });
        round_trip_model_comparison.Compare(_legacy, changed).IsEquivalent.ShouldBeFalse();
    }

    [Fact]
    void should_not_erase_a_command_calendar()
    {
        var changed = ExecutableSemanticModel.Create(
            _utc.LanguageVersion,
            _utc.SemanticVersion,
            comparison_profile_models.Commands(_utc.Application, command => command with { Calendar = new("Europe/Oslo", null) }));
        round_trip_model_comparison.Compare(_legacy, changed).IsEquivalent.ShouldBeFalse();
    }
}
#endif
