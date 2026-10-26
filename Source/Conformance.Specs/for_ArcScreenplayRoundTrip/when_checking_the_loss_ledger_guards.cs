// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_checking_the_loss_ledger_guards
{
    static readonly Lazy<RoundTripObservation> _observation = new(() => arc_round_trip.Run(round_trip_fixtures.Inline(round_trip_fixtures.FilteredReactor)));

    [Fact]
    void should_name_the_unledgered_path_when_an_entry_is_removed()
    {
        var ledger = arc_round_trip_ledger.Load();
        var entry = ledger.Losses.First(loss => loss.Fixture == _observation.Value.Fixture && loss.Change == SemanticLossChange.Missing);
        var changed = ledger with { Losses = ledger.Losses.Remove(entry) };
        var verdict = changed.Reconcile([_observation.Value], requireCompleteRun: false);

        Assert.Contains(verdict.Unledgered, finding => finding.Fixture == entry.Fixture && finding.Path == entry.Path);
        Assert.False(verdict.IsClean);
    }

    [Fact]
    void should_reject_a_stale_entry()
    {
        var ledger = arc_round_trip_ledger.Load();
        var entry = ledger.Losses.First(loss => loss.Fixture == _observation.Value.Fixture) with { Path = "/application/not-a-real-path", Change = SemanticLossChange.Missing };
        var changed = ledger with { Losses = ledger.Losses.Add(entry) };
        var verdict = changed.Reconcile([_observation.Value], requireCompleteRun: false);

        Assert.Contains(verdict.Stale, finding => finding.Path == entry.Path);
        Assert.False(verdict.IsClean);
    }

    [Fact]
    void should_reject_one_entry_more_than_the_budget()
    {
        var ledger = arc_round_trip_ledger.Load();
        var changed = ledger with { Losses = ledger.Losses.Add(ledger.Losses[0]) };
        var verdict = changed.Reconcile([_observation.Value], requireCompleteRun: false);

        Assert.NotEmpty(verdict.OverBudget);
        Assert.False(verdict.IsClean);
    }

    [Fact]
    void should_reject_an_empty_fixture_set()
    {
        var verdict = arc_round_trip_ledger.Load().Reconcile([], requireCompleteRun: true, []);

        Assert.Contains(verdict.Vacuous, finding => finding.Message == "No fixture was observed.");
        Assert.False(verdict.IsClean);
    }

    [Fact]
    void should_reject_zero_comparisons_even_with_an_observation()
    {
        var observation = _observation.Value with { Comparison = null, RecoveryCompileDiagnostics = ["PLAY0001"] };
        var verdict = arc_round_trip_ledger.Load().Reconcile([observation], requireCompleteRun: true, [observation.Fixture]);

        Assert.Equal(1, verdict.Executed);
        Assert.Equal(0, verdict.Compared);
        Assert.Contains(verdict.Vacuous, finding => finding.Message.StartsWith("No observation was compared", StringComparison.Ordinal));
        Assert.False(verdict.IsClean);
    }
}
#endif
