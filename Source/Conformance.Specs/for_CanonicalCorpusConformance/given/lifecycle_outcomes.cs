// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public static class lifecycle_outcomes
{
    const string Gap = "gap:lifecycle-specifications-failed";

    public static string Observe(conformance_vector vector, IReadOnlyList<SemanticSpecificationRunRecord> results) =>
        Observe(vector, results, EsmGoldenVectors.LifecycleDispositions.Where(entry => entry.VectorId == vector.Name).ToArray());

    public static string Observe(
        conformance_vector vector,
        IReadOnlyList<SemanticSpecificationRunRecord> results,
        IReadOnlyList<EsmLifecycleVectorDisposition> dispositions)
    {
        if (dispositions.Count == 0)
        {
            if (results.Count == 0) return "n/a:no-specifications";
            return results.All(Passed) ? "supported" : Gap;
        }

        if (!dispositions.All(entry => MatchesVector(vector, entry)) ||
            dispositions.Select(entry => (entry.OwnerKind, entry.Owner, entry.Specification)).Distinct().Count() != dispositions.Count)
        {
            return Gap;
        }

        var failures = results.Where(result => !Passed(result)).ToArray();
        return failures.Length == dispositions.Count && dispositions.All(entry => failures.Count(result => MatchesFailure(vector, result, entry)) == 1)
            ? "expected-failure"
            : Gap;
    }

    static bool Passed(SemanticSpecificationRunRecord result) => result.Outcome == SemanticSpecificationOutcome.Passed && result.Failures.Count == 0;

    static bool MatchesVector(conformance_vector vector, EsmLifecycleVectorDisposition entry) =>
        entry.VectorId == vector.Name && entry.SemanticVersion == vector.Model.SemanticVersion &&
        entry.SemanticRevision == vector.Model.Revision && entry.Sha256 == vector.GoldenSha256 &&
        !string.IsNullOrWhiteSpace(entry.Reason) && !entry.ExpectedFailures.IsDefaultOrEmpty &&
        canonical_corpus_vectors.All.Any(replacement => replacement.Name == entry.ReplacementVectorId) &&
        !EsmGoldenVectors.LifecycleDispositions.Any(other => other.VectorId == entry.ReplacementVectorId);

    static bool MatchesFailure(conformance_vector vector, SemanticSpecificationRunRecord result, EsmLifecycleVectorDisposition entry) =>
        result.Outcome == SemanticSpecificationOutcome.Failed && result.SliceKind == StageKind(entry.OwnerKind) &&
        result.SliceId == $"{vector.Model.Application.Id}/{entry.Owner}" && result.Name == entry.Specification &&
        result.SpecificationId == $"{vector.Model.Application.Id}/{StageKind(entry.OwnerKind)}/{entry.Owner}/{entry.Specification}" &&
        result.Failures.SequenceEqual(entry.ExpectedFailures, StringComparer.Ordinal);

    static string? StageKind(string ownerKind) => ownerKind switch
    {
        "state machine" => "StateMachine",
        "workflow" => "Workflow",
        _ => null
    };
}
#endif
