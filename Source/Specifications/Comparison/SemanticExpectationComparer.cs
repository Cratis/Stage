// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticExpectationComparer
{
    // Mirrors SemanticSpecificationRunner.Compare/CompareFacts for the admitted fact/error subset.
    internal static IReadOnlyList<string> Compare(SemanticSpecification expected, IReadOnlyList<SemanticSpecificationEvent> actual, string? rejection)
    {
        var failures = new List<string>();
        if (expected.ThenErrors.Length > 0)
        {
            if (rejection is null)
            {
                failures.Add("Expected Rejected, got Accepted.");
            }
            else if (expected.ThenErrors[0].Code is not null)
            {
                failures.Add($"Expected rejection code '{expected.ThenErrors[0].Code}', got ''.");
            }
            else if (expected.ThenErrors[0].Message is { } message && message != rejection)
            {
                failures.Add($"Expected rejection message '{message}', got '{rejection}' (string key: {rejection.StartsWith("$strings.", StringComparison.Ordinal)}).");
            }

            return failures;
        }

        if (rejection is not null)
        {
            failures.Add("Expected Accepted, got Rejected.");
            return failures;
        }

        if (expected.ThenEvents.Length != actual.Count)
        {
            failures.Add($"Expected {expected.ThenEvents.Length} fact(s), got {actual.Count}.");
            return failures;
        }

        var remaining = actual.ToList();
        for (var index = 0; index < expected.ThenEvents.Length; index++)
        {
            var assertion = expected.ThenEvents[index];
            var match = expected.ThenEventsInAnyOrder ? remaining.FindIndex(candidate => Matches(assertion, candidate)) : index;
            if (match < 0 || !Matches(assertion, expected.ThenEventsInAnyOrder ? remaining[match] : actual[index]))
            {
                failures.Add($"Fact at index {index} does not match the expected event contract and values.");
            }

            if (expected.ThenEventsInAnyOrder && match >= 0) remaining.RemoveAt(match);
        }

        if (expected.When?.EventSource is { } source && actual.Any(fact => fact.EventSource is null || SemanticRunContext.Canonical(fact.EventSource.Value) != SemanticRunContext.Canonical(source.Value)))
        {
            failures.Add("Produced fact destination does not match the specification command event source.");
        }

        return failures;
    }

    static bool Matches(SemanticSpecificationEvent expected, SemanticSpecificationEvent actual) =>
        expected.EventContract == actual.EventContract && expected.Values.Length == actual.Values.Length &&
        expected.Values.All(value => actual.Values.Any(candidate => candidate.TargetProperty == value.TargetProperty && SemanticRunContext.Canonical(candidate.Value) == SemanticRunContext.Canonical(value.Value))) &&
        (expected.EventSource is null || (actual.EventSource is not null && expected.EventSource.Type == actual.EventSource.Type && SemanticRunContext.Canonical(expected.EventSource.Value) == SemanticRunContext.Canonical(actual.EventSource.Value)));
}
