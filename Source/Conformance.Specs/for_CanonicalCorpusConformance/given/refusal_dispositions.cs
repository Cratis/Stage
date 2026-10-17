// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public static class refusal_dispositions
{
    public static readonly refusal_fingerprint InvalidGeneration = new("STAGE-ESM-019", null, "sem1:25bdf5d2a8f97601c41d6bc30d4da1d2fd4123e1ebf894d79f96b9103b85caf4");

    public static IReadOnlyList<string> Problems(string vector, conformance_surface surface, expected_conformance_cell expected, conformance_observation actual)
    {
        var problems = new List<string>();
        void Add(string problem) => problems.Add($"{vector} / {surface}: {problem}");
        if (!string.Equals(expected.Outcome, actual.Outcome, StringComparison.Ordinal))
        {
            Add($"changed-outcome(expected {expected.Outcome}, actual {actual.Outcome})");
        }

        foreach (var record in expected.Refusals.Where(_ => expected.Outcome != "refused"))
        {
            Add($"records-on-unrefused-cell({record.Fingerprint})");
        }

        // The corpus explicitly expects these refusals; they must not acquire tracking records.
        if (expected.Outcome == "expected-refusal" && actual.Outcome == "expected-refusal")
        {
            return problems;
        }

        foreach (var fingerprint in actual.Refusals.Where(item => string.IsNullOrWhiteSpace(item.Artifact) || item.Artifact == "none"))
        {
            Add($"refusal-without-artifact({fingerprint.Code})");
        }

        foreach (var record in expected.Refusals.Where(item => string.IsNullOrWhiteSpace(item.Fingerprint.Artifact) || item.Fingerprint.Artifact == "*"))
        {
            Add($"code-only-record({record.Fingerprint.Code})");
        }

        foreach (var group in expected.Refusals.GroupBy(record => record.Fingerprint).Where(group => group.Count() > 1))
        {
            Add($"duplicate-record({group.Key})");
        }

        var recorded = expected.Refusals.Select(record => record.Fingerprint).ToHashSet();
        foreach (var fingerprint in actual.Refusals.Except(recorded))
        {
            Add($"unrecorded-refusal({fingerprint})");
        }

        foreach (var fingerprint in recorded.Except(actual.Refusals))
        {
            Add($"stale-record({fingerprint})");
        }

        foreach (var record in expected.Refusals)
        {
            var fingerprint = record.Fingerprint;
            if (record.Issue is not > 0 && string.IsNullOrWhiteSpace(record.ByDesign))
            {
                Add($"missing-disposition({fingerprint})");
            }

            if (record.Issue is not null && record.ByDesign is not null)
            {
                Add($"conflicting-disposition({fingerprint})");
            }

            if (record.ByDesign is not null && (string.IsNullOrWhiteSpace(record.ByDesign) || vector != "GenerationFixturesCorpus.V21" || surface != conformance_surface.Renderer || fingerprint != InvalidGeneration))
            {
                Add($"unauthorized-by-design({fingerprint})");
            }

            if (record.Issue == 500 && fingerprint.Code != "STAGE-ESM-011")
            {
                Add($"issue-500-only-tracks-STAGE-ESM-011({fingerprint})");
            }
        }

        return problems;
    }
}
#endif
