// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_matching_lifecycle_dispositions : Specification
{
    conformance_vector _vector = null!;
    SemanticSpecificationRunRecord[] _results = null!;
    EsmLifecycleVectorDisposition[] _dispositions = null!;

    void Establish()
    {
        _vector = canonical_corpus_vectors.All.Single(vector => vector.Name == "EsmGoldenVectors.V36");
        var plan = SemanticExecutionPlan.Compile(_vector.Model).Plan!;
        _results = new SemanticSpecificationExecutor().Run(plan, new([]), new()).GetAwaiter().GetResult().Results.ToArray();
        _dispositions = EsmGoldenVectors.LifecycleDispositions.Where(entry => entry.VectorId == _vector.Name).ToArray();
    }

    [Fact]
    void should_match_only_the_published_failures() => Assert.True(
        lifecycle_outcomes.Observe(_vector, _results) == "expected-failure",
        System.Text.Json.JsonSerializer.Serialize(_results));

    [Theory]
    [InlineData("vector")]
    [InlineData("version")]
    [InlineData("revision")]
    [InlineData("hash")]
    [InlineData("owner-kind")]
    [InlineData("owner")]
    [InlineData("specification")]
    [InlineData("message")]
    [InlineData("replacement")]
    [InlineData("reason")]
    void should_reject_a_changed_disposition(string field)
    {
        var original = _dispositions[0];
        var changed = field switch
        {
            "vector" => original with { VectorId = "another-vector" },
            "version" => original with { SemanticVersion = new(99, 0) },
            "revision" => original with { SemanticRevision = EsmGoldenVectors.LifecycleV36R2.SemanticRevision },
            "hash" => original with { Sha256 = new string('0', 64) },
            "owner-kind" => original with { OwnerKind = "state machine" },
            "owner" => original with { Owner = "another-owner" },
            "specification" => original with { Specification = "another-specification" },
            "message" => original with { ExpectedFailures = ["another failure"] },
            "replacement" => original with { ReplacementVectorId = "missing-replacement" },
            _ => original with { Reason = string.Empty }
        };
        lifecycle_outcomes.Observe(_vector, _results, [changed, .. _dispositions.Skip(1)]).ShouldEqual("gap:lifecycle-specifications-failed");
    }

    [Fact]
    void should_reject_an_unused_disposition() =>
        lifecycle_outcomes.Observe(_vector, [.. _results.Where(result => result.Name != _dispositions[0].Specification)]).ShouldEqual("gap:lifecycle-specifications-failed");

    [Fact]
    void should_reject_an_additional_failure() =>
        lifecycle_outcomes.Observe(_vector, [.. _results, _results.First(result => result.Outcome == SemanticSpecificationOutcome.Failed)]).ShouldEqual("gap:lifecycle-specifications-failed");

    [Fact]
    void should_reject_duplicate_dispositions() =>
        lifecycle_outcomes.Observe(_vector, _results, [.. _dispositions, _dispositions[0]]).ShouldEqual("gap:lifecycle-specifications-failed");

    [Fact]
    void should_not_waive_failures_without_a_disposition() =>
        lifecycle_outcomes.Observe(_vector, _results, []).ShouldEqual("gap:lifecycle-specifications-failed");

    [Fact]
    void should_reject_changed_failure_order()
    {
        var messages = new[] { "first failure", "second failure" };
        EsmLifecycleVectorDisposition[] entries = [_dispositions[0] with { ExpectedFailures = [.. messages] }, .. _dispositions.Skip(1)];
        var results = _results.Select(result => result.Name == entries[0].Specification ? result with { Failures = messages } : result).ToArray();
        lifecycle_outcomes.Observe(_vector, results, entries).ShouldEqual("expected-failure");
        var reversed = results.Select(result => result.Name == entries[0].Specification ? result with { Failures = messages.Reverse().ToArray() } : result).ToArray();
        lifecycle_outcomes.Observe(_vector, reversed, entries).ShouldEqual("gap:lifecycle-specifications-failed");
    }
}
#endif
