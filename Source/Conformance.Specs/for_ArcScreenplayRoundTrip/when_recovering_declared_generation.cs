// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_declared_generation
{
    [Fact]
    void should_retain_the_contextual_generation_rule_body()
    {
        var observation = generation_round_trips.Get("GenerationSupportCorpus.V21");
        Assert.Contains("generate rule ", observation.RecoveredSource);
        Assert.Contains("return context.Tenant.ToString();", observation.RecoveredSource);
    }

    [Theory]
    [InlineData("GeneratedValuesCorpus.V15", "UserId")]
    [InlineData("GeneratedValuesCorpus.V15", "BareTypeId")]
    [InlineData("GeneratedValuesCorpus.V15", "HexReference")]
    [InlineData("GeneratedValuesCorpus.V15", "EncodedRandom")]
    [InlineData("GenerationSupportCorpus.V21", "TicketNumber")]
    [InlineData("GenerationSupportCorpus.V21", "TenantOrderId")]
    void should_recover_the_complete_concept_including_its_strategy(string fixture, string concept)
    {
        var observation = generation_round_trips.Get(fixture);
        var expected = Assert.Single(observation.Fixture.Model.Application.Concepts, candidate => candidate.Name == concept);
        Assert.NotNull(expected.Generation);
        Assert.Empty(Differences(observation, concept));
    }

    [Fact]
    void should_recover_the_ordinary_person_identifier_without_inventing_generation()
    {
        var observation = generation_round_trips.Get("EsmGoldenVectors.V38");
        Assert.Null(Assert.Single(observation.Fixture.Model.Application.Concepts, concept => concept.Name == "PersonId").Generation);
        Assert.Empty(Differences(observation, "PersonId"));
    }

    [Theory]
    [InlineData("Base32Reference", 5)]
    [InlineData("BinaryCode", 5)]
    [InlineData("SortableId", 3)]
    [InlineData("StringUuid", 4)]
    void should_report_the_exact_ledgered_prefix_and_case_limitation(string concept, int count)
    {
        var observation = generation_round_trips.Get("GeneratedValuesCorpus.V15");
        AssertLedgered(observation, concept, count, "Cratis/Arc#3987");
        var kind = Assert.Single(Differences(observation, concept), difference => difference.Path.EndsWith("/generation/kind", StringComparison.Ordinal));
        Assert.Equal("\"implementation\"", kind.Actual);
    }

    [Fact]
    void should_report_the_existing_checksum_validation_limitation() =>
        AssertLedgered(generation_round_trips.Get("GenerationSupportCorpus.V21"), "InvoiceRef", 2, "Cratis/Arc#3554");

    static void AssertLedgered(generation_round_trips.observation observation, string concept, int count, string issue)
    {
        var differences = Differences(observation, concept);
        Assert.Equal(count, differences.Length);
        var ledger = arc_round_trip_ledger.Load();
        foreach (var difference in differences)
        {
            var entry = Assert.Single(ledger.Losses, entry => entry.Fixture == observation.Fixture.Name && entry.Path == difference.Path);
            Assert.Equal(difference.Kind.ToString(), entry.Change?.ToString());
            Assert.Equal<RoundTripDirection?>(RoundTripDirection.Adapter, entry.Direction);
            Assert.Equal(issue, entry.Issue);
        }
    }

    static SemanticDifference[] Differences(generation_round_trips.observation observation, string concept)
    {
        var comparison = Assert.IsType<SemanticModelComparison>(observation.Result.Comparison);
        var path = $"/application/concepts[{concept}]";
        return [.. comparison.Differences.Where(difference => difference.Path == path || difference.Path.StartsWith(path + "/", StringComparison.Ordinal))];
    }
}
#endif
