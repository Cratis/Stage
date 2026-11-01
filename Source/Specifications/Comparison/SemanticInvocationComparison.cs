// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Comparison;

/// <summary>
/// Compares the invocations a specification states with `then invokes` against those its slice's reactions made.
/// </summary>
/// <remarks>
/// Mirrors Screenplay's SemanticInvocationComparison (Screenplay 4.247.0, decision 0115): count and order must match - under
/// `then events in any order` the invocations match as a multiset - and only the stated values are compared.
/// </remarks>
internal static class SemanticInvocationComparison
{
    /// <summary>
    /// Compares stated invocations with recorded ones.
    /// </summary>
    /// <param name="plan">The plan, for naming commands and properties in failures.</param>
    /// <param name="expected">The stated invocations, in authored order.</param>
    /// <param name="actual">The recorded invocations, in invocation order.</param>
    /// <param name="inAnyOrder">Whether the invocations match without regard to order.</param>
    /// <param name="failures">The failures to add to.</param>
    internal static void Compare(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticSpecificationInvocation> expected,
        IReadOnlyList<SemanticRecordedInvocation> actual,
        bool inAnyOrder,
        List<string> failures)
    {
        if (expected.Length != actual.Count)
        {
            failures.Add($"Expected {expected.Length} invocation(s), got {actual.Count}. Expected: {Describe(plan, expected)}. Actual: {Describe(plan, actual)}.");
            return;
        }

        if (!inAnyOrder)
        {
            for (var index = 0; index < expected.Length; index++)
            {
                if (!Matches(expected[index], actual[index]))
                {
                    failures.Add($"Invocation at index {index} does not match the expected command and values. Expected: {Describe(plan, expected[index])}. Actual: {Describe(plan, actual[index])}.");
                }
            }

            return;
        }

        // Augmenting paths reassign earlier matches instead of consuming the first candidate greedily.
        var assignments = Enumerable.Repeat(-1, actual.Count).ToArray();
        for (var index = 0; index < expected.Length; index++)
        {
            if (!Assign(index, new bool[actual.Count]))
            {
                failures.Add($"Invocation at index {index} was not made. Expected: {Describe(plan, expected[index])}. Actual: {Describe(plan, actual)}.");
            }
        }

        bool Assign(int expectation, bool[] visited)
        {
            for (var made = 0; made < actual.Count; made++)
            {
                if (visited[made] || !Matches(expected[expectation], actual[made])) continue;
                visited[made] = true;
                if (assignments[made] < 0 || Assign(assignments[made], visited))
                {
                    assignments[made] = expectation;
                    return true;
                }
            }

            return false;
        }
    }

    static bool Matches(SemanticSpecificationInvocation expected, SemanticRecordedInvocation actual) =>
        expected.Command == actual.Command && expected.Values.All(value =>
            actual.Values.SingleOrDefault(candidate => candidate.TargetProperty == value.TargetProperty) is { } candidate &&
            SemanticExpectationComparer.AreEqual(value.Value, candidate.Value));

    static string Describe(SemanticExecutionPlan plan, ImmutableArray<SemanticSpecificationInvocation> invocations) =>
        invocations.IsEmpty ? "none" : string.Join(", ", invocations.Select(invocation => Describe(plan, invocation)));

    static string Describe(SemanticExecutionPlan plan, IReadOnlyList<SemanticRecordedInvocation> invocations) =>
        invocations.Count == 0 ? "none" : string.Join(", ", invocations.Select(invocation => Describe(plan, invocation)));

    static string Describe(SemanticExecutionPlan plan, SemanticSpecificationInvocation invocation) => Describe(plan, invocation.Command, invocation.Values);

    static string Describe(SemanticExecutionPlan plan, SemanticRecordedInvocation invocation) =>
        $"{Describe(plan, invocation.Command, invocation.Values)}{(invocation.Accepted ? string.Empty : " (refused)")}";

    static string Describe(SemanticExecutionPlan plan, SemanticId command, ImmutableArray<SemanticPropertyValue> values)
    {
        if (!plan.Commands.TryGetValue(command, out var declared)) return command.ToString();
        var names = declared.Properties.ToDictionary(property => property.Id, property => property.Name);

        return $"{declared.Name}({string.Join(", ", values.Select(value => $"{names.GetValueOrDefault(value.TargetProperty, value.TargetProperty.ToString())} = {Describe(value.Value)}"))})";
    }

    static string Describe(SemanticValue value) => value switch
    {
        SemanticNullValue => "null",
        SemanticTextValue text => $"\"{text.Value}\"",
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        SemanticArrayValue array => $"[{string.Join(", ", array.Values.Select(Describe))}]",
        _ => "{...}"
    };
}
