// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

// Mirrors Screenplay v4.24.0 SemanticConstraintEnforcement: candidate order, sorted constraint names,
// replacement per owner, releasing events and invariant casing all matter for atomic batches.
internal static class SemanticConstraintEvaluator
{
    internal static SemanticConstraint? FindViolation(SemanticExecutionPlan plan, IReadOnlyList<Fact> history, IReadOnlyList<Fact> candidates) =>
        FindViolations(plan, history, candidates).FirstOrDefault();

    internal static IEnumerable<SemanticConstraint> FindViolations(SemanticExecutionPlan plan, IReadOnlyList<Fact> history, IReadOnlyList<Fact> candidates)
    {
        var constraints = plan.Constraints.Values.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var preceding = history.Concat(candidates.Take(index)).ToArray();
            foreach (var constraint in constraints.Where(value => Target(value, candidate.Contract) is not null))
            {
                if (constraint.Kind switch
                {
                    SemanticConstraintKind.UniquePropertyValue => ViolatesValue(constraint, candidate, preceding),
                    SemanticConstraintKind.UniqueEventOccurrence => ViolatesOccurrence(constraint, candidate, preceding),
                    _ => throw new UnsupportedSemanticMapping()
                })
                {
                    yield return constraint;
                }
            }
        }
    }

    internal static string Message(SemanticConstraint constraint) => constraint.Message ?? (constraint.Kind == SemanticConstraintKind.UniquePropertyValue
        ? $"Constraint '{constraint.Name}' is violated: another event source already holds the constrained value."
        : $"Constraint '{constraint.Name}' is violated: the event source already has the constrained event.");

    internal static bool ViolatesValue(SemanticConstraint constraint, Fact candidate, IReadOnlyList<Fact> history)
    {
        var value = Constrained(constraint, candidate);
        if (value.Length == 0)
        {
            return false;
        }
        var claims = new List<(SemanticValue Owner, SemanticValue[] Values)>();
        foreach (var fact in history)
        {
            var owner = fact.Destination is SemanticNullValue ? candidate.Destination : fact.Destination;
            var releases = constraint.ReleasedBy.Contains(fact.Contract);
            if (!releases && Target(constraint, fact.Contract) is null)
            {
                continue;
            }
            claims.RemoveAll(claim => SemanticExpectationComparer.AreEqual(claim.Owner, owner));
            var claimed = releases ? [] : Constrained(constraint, fact);
            if (claimed.Length > 0)
            {
                claims.Add((owner, claimed));
            }
        }

        return claims.Exists(claim => !SemanticExpectationComparer.AreEqual(claim.Owner, candidate.Destination) && SameValue(claim.Values, value, constraint.IgnoreCasing));
    }

    internal static bool SameValue(SemanticValue[] claimed, SemanticValue[] value, bool ignoreCasing) =>
        claimed.Length == value.Length && claimed.Zip(value).All(pair => (pair.First, pair.Second, ignoreCasing) switch
        {
            (SemanticTextValue a, SemanticTextValue b, true) => string.Equals(a.Value.ToLowerInvariant(), b.Value.ToLowerInvariant(), StringComparison.Ordinal),
            _ => SemanticExpectationComparer.AreEqual(pair.First, pair.Second)
        });

    internal static bool ViolatesOccurrence(SemanticConstraint constraint, Fact candidate, IReadOnlyList<Fact> history)
    {
        var open = false;
        foreach (var fact in history.Where(fact => SemanticExpectationComparer.AreEqual(fact.Destination is SemanticNullValue ? candidate.Destination : fact.Destination, candidate.Destination)))
        {
            if (constraint.ReleasedBy.Contains(fact.Contract)) open = false;
            else if (Target(constraint, fact.Contract) is not null) open = true;
        }

        return open;
    }

    internal static SemanticValue[] Constrained(SemanticConstraint constraint, Fact fact) =>
        [.. Target(constraint, fact.Contract)!.Properties.Select(property => fact.Values.FirstOrDefault(value => value.TargetProperty == property)?.Value)
            .Where(value => value is not null and not SemanticNullValue).Select(value => value!)];

    internal static SemanticConstraintTarget? Target(SemanticConstraint constraint, SemanticId contract) => constraint.Targets.FirstOrDefault(target => target.EventContract == contract);

    internal sealed record Fact(SemanticId Contract, SemanticValue Destination, IReadOnlyList<SemanticPropertyValue> Values);
}
