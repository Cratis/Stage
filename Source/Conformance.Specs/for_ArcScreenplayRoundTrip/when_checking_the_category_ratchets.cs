// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_checking_the_category_ratchets
{
    [Fact]
    void should_accept_the_reviewed_nonempty_categories_at_both_caps()
    {
        var counts = arc_round_trip_ledger.ValidateCategories(arc_round_trip_ledger.Load().Losses);

        Assert.Equal(191, counts.Semantic);
        Assert.Equal(42, counts.Diagnostic);
        Assert.Equal(0, counts.Unsupported);
        Assert.True(counts.IsWithinBudget);
    }

    [Fact]
    void should_reject_192_semantic_even_with_only_41_diagnostics()
    {
        var counts = Counts(192, 41);

        Assert.Equal(233, counts.Semantic + counts.Diagnostic);
        Assert.Equal(192, counts.Semantic);
        Assert.Equal(41, counts.Diagnostic);
        Assert.False(counts.IsWithinBudget);
    }

    [Fact]
    void should_reject_43_diagnostics_even_with_only_190_semantic()
    {
        var counts = Counts(190, 43);

        Assert.Equal(233, counts.Semantic + counts.Diagnostic);
        Assert.Equal(190, counts.Semantic);
        Assert.Equal(43, counts.Diagnostic);
        Assert.False(counts.IsWithinBudget);
    }

    [Fact]
    void should_allow_both_categories_to_decrease_without_spending_the_headroom()
    {
        var counts = Counts(190, 41);

        Assert.True(counts.IsWithinBudget);
        Assert.Equal(191, arc_round_trip_ledger.MaximumSemanticEntries);
        Assert.Equal(42, arc_round_trip_ledger.MaximumDiagnosticEntries);
    }

    [Fact]
    void should_reject_an_empty_category_check()
    {
        var counts = arc_round_trip_ledger.ValidateCategories([]);

        Assert.Equal(new ledger_category_counts(0, 0, 0), counts);
        Assert.False(counts.IsWithinBudget);
    }

    [Fact]
    void should_classify_diagnostic_separately_from_every_other_supported_kind()
    {
        foreach (var change in Enum.GetValues<SemanticLossChange>())
        {
            var entry = Entry(change);
            var counts = arc_round_trip_ledger.ValidateCategories([entry]);
            var expected = change == SemanticLossChange.Diagnostic ? new ledger_category_counts(0, 1, 0) : new ledger_category_counts(1, 0, 0);

            Assert.Equal(expected, counts);
            Assert.True(counts.IsWithinBudget);
        }
    }

    [Fact]
    void should_refuse_null_or_unknown_kinds_instead_of_counting_them_as_semantic()
    {
        foreach (var change in new SemanticLossChange?[] { null, (SemanticLossChange)999 })
        {
            var counts = arc_round_trip_ledger.ValidateCategories([Entry(change)]);

            Assert.Equal(new ledger_category_counts(0, 0, 1), counts);
            Assert.False(counts.IsWithinBudget);
        }
    }

    static ledger_category_counts Counts(int semantic, int diagnostic) => arc_round_trip_ledger.ValidateCategories(
        Enumerable.Repeat(Entry(SemanticLossChange.Changed), semantic).Concat(Enumerable.Repeat(Entry(SemanticLossChange.Diagnostic), diagnostic)));

    static SemanticLossEntry Entry(SemanticLossChange? change) => new("CategoryGuard", change, "/category", RoundTripDirection.Adapter, [], "Cratis/Arc#3559", null);
}
#endif
