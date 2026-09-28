// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticExpectationComparer
{
    // Mirrors SemanticSpecificationRunner.Compare/CompareFacts for the admitted fact/error subset.
    internal static IReadOnlyList<string> Compare(SemanticSpecification expected, IReadOnlyList<SemanticSpecificationEvent> actual, IReadOnlyList<SemanticValue> destinations, string? rejection, string? rejectionCode = null, bool denied = false)
    {
        var failures = new List<string>();
        if (expected.ThenDenied)
        {
            if (!denied) failures.Add($"Expected Unauthorized, got {(rejection is null ? "Accepted" : "Rejected (Validation)")}.");
            return failures;
        }
        if (expected.ThenErrors.Length > 0)
        {
            if (rejection is null || denied)
            {
                failures.Add("Expected Rejected, got Accepted.");
            }
            else
            {
                if (expected.ThenErrors[0].Code is { } code && code != rejectionCode)
                {
                    failures.Add($"Expected rejection code '{code}', got '{rejectionCode}'.");
                }
                if (expected.ThenErrors[0].Message is { } message && message != rejection)
                {
                    failures.Add($"Expected rejection message '{message}', got '{rejection}' (string key: {rejection.StartsWith("$strings.", StringComparison.Ordinal)}).");
                }
            }

            return failures;
        }

        if (rejection is not null)
        {
            failures.Add("Expected Accepted, got Rejected.");
            return failures;
        }

        if (expected.WhenAppended is not null && expected.ThenEvents.IsEmpty) return failures;

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

        if (expected.When?.EventSource is { } source && destinations.Any(destination => !AreEqual(destination, source.Value)))
        {
            failures.Add("Produced fact destination does not match the specification command event source.");
        }

        return failures;
    }

    internal static IReadOnlyList<string> CompareProjections(SemanticSpecification expected, IReadOnlyList<SemanticRunProjections.Projected> projected, SemanticExecutionPlan plan)
    {
        var failures = new List<string>();
        foreach (var state in expected.ThenReadModels)
        {
            if (!MatchesState(state, projected))
            {
                failures.Add($"Expected read model '{state.ReadModel}' with key '{state.Key}' was not found with matching values.");
            }
        }
        for (var index = 0; index < expected.ThenQueries.Length; index++)
        {
            var query = expected.ThenQueries[index];
            var readModel = plan.Queries[query.Query].ReadModel;
            var rows = projected.Where(row => row.ReadModel == readModel && AreEqual(row.Key, query.Key)).ToArray();
            if (rows.Length != query.Results.Length || query.Results.Any(result => !MatchesState(result, rows, query.Exactly)))
            {
                failures.Add($"Query result at index {index} expected {query.Results.Length} row(s) with matching values, got {rows.Length}.");
            }
        }
        return failures;
    }

    internal static bool Matches(SemanticSpecificationEvent expected, SemanticSpecificationEvent actual) =>
        expected.EventContract == actual.EventContract && expected.Values.Length == actual.Values.Length &&
        expected.Values.All(value => actual.Values.Any(candidate => candidate.TargetProperty == value.TargetProperty && AreEqual(candidate.Value, value.Value))) &&
        (expected.EventSource is null || (actual.EventSource is not null && expected.EventSource.Type == actual.EventSource.Type && AreEqual(expected.EventSource.Value, actual.EventSource.Value)));

    // Match Screenplay v4.24.0 SemanticValueRules.AreEqual, including numeric scale and nested values.
    internal static bool AreEqual(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticNullValue, SemanticNullValue) => true,
        (SemanticTextValue a, SemanticTextValue b) => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
        (SemanticNumberValue a, SemanticNumberValue b) => a.Value == b.Value,
        (SemanticBooleanValue a, SemanticBooleanValue b) => a.Value == b.Value,
        (SemanticArrayValue a, SemanticArrayValue b) => a.Values.Length == b.Values.Length && a.Values.Zip(b.Values).All(pair => AreEqual(pair.First, pair.Second)),
        (SemanticCompositeValue a, SemanticCompositeValue b) => a.Properties.Length == b.Properties.Length &&
            a.Properties.All(property => b.Properties.Any(candidate => candidate.TargetProperty == property.TargetProperty && AreEqual(property.Value, candidate.Value))),
        _ => false
    };

    static bool MatchesState(SemanticSpecificationReadModel state, IReadOnlyList<SemanticRunProjections.Projected> rows, bool exactly = false) =>
        rows.Any(row => row.ReadModel == state.ReadModel && AreEqual(row.Key, state.Key) &&
            (!(state.Exactly || exactly) || state.Values.Length == row.Values.Length) &&
            state.Values.All(expected => row.Values.Any(actual => actual.TargetProperty == expected.TargetProperty && AreEqual(actual.Value, expected.Value))));
}
