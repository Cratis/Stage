// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_the_common_profile_is_selected : Specification
{
    [Fact]
    void should_use_the_supported_v48_pair()
    {
        var profile = round_trip_model_profile.Common;
        profile.Language.ShouldEqual(LanguageVersion.V48);
        profile.Semantic.ShouldEqual(SemanticVersion.V48);
        EsmSchemaV48Support.Supports(profile.Language, profile.Semantic).ShouldBeTrue();
    }

    [Fact]
    void should_cover_every_actually_loaded_eligible_fixture()
    {
        var table = expected_conformance_table.Load();
        var ledger = arc_round_trip_ledger.Load();
        var observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fixture in round_trip_fixtures.Fixtures(table, ledger.ExcludedFixtures.Keys))
        {
            Assert.True(observed.Add(fixture.Name), $"Duplicate fixture '{fixture.Name}'.");
            Assert.True(round_trip_model_profile.Covers(fixture.Model),
                $"Fixture '{fixture.Name}' declares ({fixture.Model.LanguageVersion}, {fixture.Model.SemanticVersion}), above the common profile {round_trip_model_profile.Common}.");
        }

        var eligible = round_trip_fixtures.EligibleNames(table).Except(ledger.ExcludedFixtures.Keys, StringComparer.Ordinal);
        Assert.NotEmpty(observed);
        Assert.Equal(eligible.Order(StringComparer.Ordinal), observed.Order(StringComparer.Ordinal));
    }
}
#endif
