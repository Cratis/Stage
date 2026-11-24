// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_replaying_date_and_reaction_fixtures
{
    [Theory]
    [InlineData("DateComparisonsCorpus.V26")]
    [InlineData("EsmGoldenVectors.V30")]
    public void should_only_have_reviewed_losses(string name)
    {
        var ledger = arc_round_trip_ledger.Load();
        var fixture = round_trip_fixtures.Fixtures(expected_conformance_table.Load(), ledger.ExcludedFixtures.Keys).Single(fixture => fixture.Name == name);
        var verdict = ledger.Reconcile([arc_round_trip.Run(fixture)], requireCompleteRun: false);

        Assert.True(verdict.IsClean, arc_round_trip_ledger.Describe(verdict.Findings));
    }
}
#endif
