// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Full semantic identities, never shortened hashes: two diagnostics sharing a code can concern different artifacts.
public record renderer_refusal(string Code, string Artifact)
{
    public override string ToString() => $"{Code}@{Artifact}";
}

public static class renderer_refusals
{
    const string Prefix = "refused:";

    // The deliberately invalid fixtures are an explicit allow-list, independent of the table's exemptions and issue coverage.
    // TenantOrderId's generation fixture in GenerationFixturesCorpus.V21; neither the vector nor the code alone grants an exemption.
    public static IReadOnlySet<renderer_refusal> AllowedByDesign { get; } = new HashSet<renderer_refusal>
    {
        new("STAGE-ESM-019", "sem1:25bdf5d2a8f97601c41d6bc30d4da1d2fd4123e1ebf894d79f96b9103b85caf4")
    };

    public static IReadOnlySet<renderer_refusal> Parse(string outcome) => !outcome.StartsWith(Prefix, StringComparison.Ordinal)
        ? new HashSet<renderer_refusal>()
        : outcome[Prefix.Length..].Split('+').Select(value => value.Split('@', 2))
            .Select(parts => new renderer_refusal(parts[0], parts.Length == 2 ? parts[1] : string.Empty)).ToHashSet();

    public static bool Matches(string expected, string actual) =>
        expected.StartsWith(Prefix, StringComparison.Ordinal) && actual.StartsWith(Prefix, StringComparison.Ordinal)
            ? Parse(expected).SetEquals(Parse(actual))
            : expected == actual;

    public static IReadOnlyList<string> CoverageProblems(string vector, expected_conformance_cell cell, string actual)
    {
        var observed = Parse(actual);
        var designs = cell.ByDesign.Select(entry => entry.Fingerprint).ToHashSet();
        var tracked = cell.Issue is not null ? cell.IssueRefusals.ToHashSet() : [];
        return [
            .. observed.Where(item => string.IsNullOrEmpty(item.Artifact)).Select(item => $"missing-artifact({item.Code})"),
            .. observed.Except(tracked).Except(designs).Select(item => $"untracked-refusal({item})"),
            .. cell.ByDesign.Where(entry => string.IsNullOrWhiteSpace(entry.Reason) ||
                    vector != "GenerationFixturesCorpus.V21" || !AllowedByDesign.Contains(entry.Fingerprint))
                .Select(entry => $"invalid-by-design-entry({entry.Fingerprint})"),
            .. designs.Except(observed).Select(item => $"unnecessary by-design entry({item})"),
            .. tracked.Except(observed).Select(item => $"unnecessary-issue-coverage({item})"),
            .. tracked.Intersect(designs).Select(item => $"issue-covers-by-design-refusal({item})"),
            .. tracked.Where(item => AllowedByDesign.Contains(item) || (cell.Issue == 500 && item.Code != "STAGE-ESM-011"))
                .Select(item => $"issue-does-not-cover-refusal({item})")
        ];
    }
}
#endif
