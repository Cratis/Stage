// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public static class refusal_dispositions
{
    public static readonly refusal_fingerprint InvalidGeneration = new("STAGE-ESM-019", null, "sem1:25bdf5d2a8f97601c41d6bc30d4da1d2fd4123e1ebf894d79f96b9103b85caf4");

    // Each entry allows only the exact vector/surface/fingerprint combinations listed here.
    static readonly (string[] Vectors, conformance_surface Surface, refusal_fingerprint[] Fingerprints)[] _allowedByDesign =
    [
        (["GenerationFixturesCorpus.V21"], conformance_surface.Renderer, [InvalidGeneration]),
        ([
            "EsmGoldenVectors.V8", "EsmGoldenVectors.V9", "EsmGoldenVectors.V10",
            "EsmGoldenVectors.V11", "EsmGoldenVectors.V12", "EsmGoldenVectors.V13",
            "EsmGoldenVectors.V14", "EsmGoldenVectors.V15", "EsmGoldenVectors.V16",
            "EsmGoldenVectors.V17", "EsmGoldenVectors.V18", "EsmGoldenVectors.V19",
            "EsmGoldenVectors.V20", "EsmGoldenVectors.V21", "EsmGoldenVectors.V22",
            "EsmGoldenVectors.V23", "EsmGoldenVectors.V24"
        ], conformance_surface.Renderer,
        [
            new("STAGE-ESM-030", null, "sem1:000000000000000000000000000000000000000000000000000000000000232c"),
            new("STAGE-ESM-030", null, "sem1:0000000000000000000000000000000000000000000000000000000000002333")
        ]),

        // A generate rule has an implementation body the interpreter never runs: the Host refuses the command by design (Stage#510).
        (["GenerationFixturesCorpus.V21", "GenerationFixturesCorpus.V21R2"], conformance_surface.Host, [new("STAGE-ESM-028", "command", "sem1:cb7e91a776653c53ea020a87d6d589bb82d105ed85303402d8081ebf9d62a964")]),

        // ESM v29 add/subtract by a product whose factors are not both properties of the event: Chronicle's projection engine adds one event property.
        (["EsmGoldenVectors.V29"], conformance_surface.Renderer, [new("STAGE-ESM-050", null, "sem1:569083a6f62ce80edfc569cac4c7b0cfa3985680155539e45d7e1cfc180f5016")])
    ];

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
            CheckCompileDisposition(record, Add);
            if (record.Issue is not > 0 && string.IsNullOrWhiteSpace(record.ByDesign))
            {
                Add($"missing-disposition({fingerprint})");
            }

            if (record.Issue is not null && record.ByDesign is not null)
            {
                Add($"conflicting-disposition({fingerprint})");
            }

            if (record.ByDesign is not null && (string.IsNullOrWhiteSpace(record.ByDesign) || !IsAllowedByDesign(vector, surface, fingerprint)))
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

    static void CheckCompileDisposition(refusal_record record, Action<string> add)
    {
        if (record.Fingerprint.Kind == "compile" && (record.Issue is not > 0 || record.ByDesign is not null))
        {
            add($"compile-record-needs-issue({record.Fingerprint})");
        }
    }

    static bool IsAllowedByDesign(string vector, conformance_surface surface, refusal_fingerprint fingerprint) =>
        _allowedByDesign.Any(entry => entry.Surface == surface && entry.Vectors.Contains(vector, StringComparer.Ordinal) && entry.Fingerprints.Contains(fingerprint));
}
#endif
