// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public static class loader_refusals
{
    public static conformance_observation Observe(IEnumerable<(string Kind, string Artifact, string Details)> issues)
    {
        var grouped = issues.GroupBy(issue => new refusal_fingerprint("STAGE-PLAN-004", issue.Kind, issue.Artifact))
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal).ToArray();
        var details = grouped.ToDictionary(
            group => group.Key,
            group => $"Occurrences: {group.Count()}{Environment.NewLine}{string.Join(Environment.NewLine, group.Select(issue => issue.Details).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))}");
        return new(grouped.Length == 0 ? "gap:loader-refused-without-issues" : "refused", [.. grouped.Select(group => group.Key)], details);
    }
}
#endif
