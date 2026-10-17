// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Count equality alone cannot prove that the executor ran the requested specifications.
public static class executor_result_ids
{
    public static IReadOnlyList<string> Problems(IEnumerable<string> planned, IEnumerable<string> results)
    {
        var expected = planned.ToHashSet(StringComparer.Ordinal);
        var actual = results.ToArray();
        var unique = actual.ToHashSet(StringComparer.Ordinal);
        return [
            .. actual.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1)
                .Select(group => $"duplicate-result-id({group.Key})"),
            .. expected.Except(unique).Order(StringComparer.Ordinal).Select(id => $"missing-result-id({id})"),
            .. unique.Except(expected).Order(StringComparer.Ordinal).Select(id => $"unknown-result-id({id})")
        ];
    }
}
#endif
