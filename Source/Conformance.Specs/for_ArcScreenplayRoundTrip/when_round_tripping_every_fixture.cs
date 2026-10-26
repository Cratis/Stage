// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

// Every eligible fixture through the Arc round trip once, compared as a whole canonical semantic model and reconciled against the reviewed
// ledger of Arc losses. A new loss, a loss that was fixed, an uncited warning and an empty run each fail here, naming what to change.
public class when_round_tripping_every_fixture
{
    static readonly Lazy<context> _context = new(() => new context());

    static context fixture => _context.Value;

    [Fact] void should_load_the_ledger_without_problems() => Assert.True(fixture.Verdict.Malformed.IsEmpty, arc_round_trip_ledger.Describe(fixture.Verdict.Malformed));
    [Fact] void should_run_the_corpus_version_the_ledger_was_written_for() => fixture.CorpusVersion.ShouldEqual(fixture.Ledger.CorpusVersion);
    [Fact] void should_compare_at_least_the_minimum_number_of_fixtures() => Assert.True(fixture.Verdict.Vacuous.IsEmpty && fixture.Verdict.Executed >= fixture.Ledger.MinimumFixtureCount, $"{fixture.Verdict.Executed} fixtures observed, {fixture.Verdict.Compared} compared, at least {fixture.Ledger.MinimumFixtureCount} expected.{Environment.NewLine}{arc_round_trip_ledger.Describe(fixture.Verdict.Vacuous)}");
    [Fact] void should_compare_every_eligible_fixture() => Assert.True(fixture.Verdict.Executed > 0 && fixture.Verdict.Executed == fixture.Eligible.Count - fixture.Ledger.ExcludedFixtures.Count, $"{fixture.Verdict.Executed} fixtures observed of {fixture.Eligible.Count} eligible with {fixture.Ledger.ExcludedFixtures.Count} excluded.");
    [Fact] void should_have_no_fixture_error() => Assert.True(fixture.FixtureErrors.Count == 0, string.Join(Environment.NewLine, fixture.FixtureErrors));
    [Fact] void should_have_no_unledgered_difference() => Assert.True(fixture.Verdict.Unledgered.IsEmpty, $"Differences no ledger entry accepts:{Environment.NewLine}{arc_round_trip_ledger.Describe(fixture.Verdict.Unledgered)}");
    [Fact] void should_have_no_stale_loss() => Assert.True(fixture.Verdict.Stale.IsEmpty, $"Ledger entries that match nothing:{Environment.NewLine}{arc_round_trip_ledger.Describe(fixture.Verdict.Stale)}");
    [Fact] void should_explain_every_loss_by_a_diagnostic() => Assert.True(fixture.Verdict.Unexplained.IsEmpty, $"Losses no diagnostic explains:{Environment.NewLine}{arc_round_trip_ledger.Describe(fixture.Verdict.Unexplained)}");
    [Fact] void should_cite_every_warning_diagnostic() => Assert.True(fixture.Verdict.Uncited.IsEmpty, $"Diagnostics no ledger entry cites:{Environment.NewLine}{arc_round_trip_ledger.Describe(fixture.Verdict.Uncited)}");
    [Fact] void should_stay_within_the_loss_budget() => Assert.True(fixture.Verdict.OverBudget.IsEmpty, arc_round_trip_ledger.Describe(fixture.Verdict.OverBudget));
    [Fact] void should_have_no_orphaned_entry_or_exclusion() => Assert.True(fixture.Verdict.Orphans.Length + fixture.Verdict.IneligibleExclusions.Length + fixture.Verdict.ObsoleteExclusions.Length + fixture.Verdict.Unobserved.Length == 0, arc_round_trip_ledger.Describe([.. fixture.Verdict.Orphans, .. fixture.Verdict.IneligibleExclusions, .. fixture.Verdict.ObsoleteExclusions, .. fixture.Verdict.Unobserved]));

    sealed class context
    {
        internal context()
        {
            var table = expected_conformance_table.Load();
            Ledger = arc_round_trip_ledger.Load();
            CorpusVersion = canonical_corpus_vectors.CorpusAssembly.GetName().Version!.ToString(3);
            Eligible = round_trip_fixtures.EligibleNames(table);
            var observations = new List<RoundTripObservation>();
            foreach (var roundTripFixture in round_trip_fixtures.Fixtures(table, Ledger.ExcludedFixtures.Keys))
            {
                try
                {
                    observations.Add(arc_round_trip.Run(roundTripFixture));
                }
                catch (Exception exception)
                {
                    FixtureErrors.Add($"{roundTripFixture.Name}: {exception.Message}");
                }
            }

            Verdict = Ledger.Reconcile(observations, requireCompleteRun: true, Eligible);
            Console.WriteLine($"Arc round trip: {Eligible.Count} eligible, {Verdict.Executed} observed, {Verdict.Compared} compared, {Ledger.Losses.Length} ledger entries.");
            foreach (var finding in Verdict.Findings)
            {
                Console.WriteLine($"  {finding.Fixture} {finding.Change} {finding.Path}: {finding.Message}");
            }
        }

        internal SemanticLossLedger Ledger { get; }
        internal string CorpusVersion { get; }
        internal IReadOnlyList<string> Eligible { get; }
        internal List<string> FixtureErrors { get; } = [];
        internal SemanticLossVerdict Verdict { get; }
    }
}
#endif
