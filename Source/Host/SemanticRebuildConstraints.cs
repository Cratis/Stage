// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host;

// Mirrors the reference's append-order constraint check. The reference enforcement is internal;
// refuse unknown shapes rather than treating a historical claim as valid by default.
internal static class SemanticRebuildConstraints
{
    internal static void Check(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts)
    {
        var history = new List<SemanticFact>();
        foreach (var fact in facts)
        {
            foreach (var constraint in plan.Constraints.Values.Where(value => Target(value, fact.EventContract) is not null)
                .OrderBy(value => value.Name, StringComparer.Ordinal))
            {
                if (constraint.Kind switch
                {
                    SemanticConstraintKind.UniquePropertyValue => ViolatesValue(constraint, fact, history),
                    SemanticConstraintKind.UniqueEventOccurrence => ViolatesOccurrence(constraint, fact, history),
                    _ => throw new SemanticWorldRebuildRefused($"Constraint '{constraint.Name}' cannot be verified on rebuild.")
                })
                {
                    throw new SemanticWorldRebuildRefused($"Historical facts violate constraint '{constraint.Name}'.");
                }
            }

            history.Add(fact);
        }
    }

    static bool ViolatesValue(SemanticConstraint constraint, SemanticFact candidate, List<SemanticFact> history)
    {
        var value = Constrained(constraint, candidate);
        if (value.IsEmpty)
        {
            return false;
        }

        var claims = new List<(SemanticValue Owner, ImmutableArray<SemanticValue> Values)>();
        foreach (var fact in history)
        {
            var releases = constraint.ReleasedBy.Contains(fact.EventContract);
            if (!releases && Target(constraint, fact.EventContract) is null)
            {
                continue;
            }

            claims.RemoveAll(claim => Equal(claim.Owner, fact.Destination));
            var claimed = releases ? [] : Constrained(constraint, fact);
            if (!claimed.IsEmpty)
            {
                claims.Add((fact.Destination, claimed));
            }
        }

        return claims.Exists(claim => !Equal(claim.Owner, candidate.Destination) &&
            claim.Values.Length == value.Length && claim.Values.Zip(value).All(pair =>
                constraint.IgnoreCasing && pair.First is SemanticTextValue first && pair.Second is SemanticTextValue second
                    ? string.Equals(first.Value.ToLowerInvariant(), second.Value.ToLowerInvariant(), StringComparison.Ordinal)
                    : Equal(pair.First, pair.Second)));
    }

    static bool ViolatesOccurrence(SemanticConstraint constraint, SemanticFact candidate, List<SemanticFact> history)
    {
        var open = false;
        foreach (var fact in history.Where(fact => Equal(fact.Destination, candidate.Destination)))
        {
            if (constraint.ReleasedBy.Contains(fact.EventContract))
            {
                open = false;
            }
            else if (Target(constraint, fact.EventContract) is not null)
            {
                open = true;
            }
        }

        return open;
    }

    static ImmutableArray<SemanticValue> Constrained(SemanticConstraint constraint, SemanticFact fact) =>
    [.. Target(constraint, fact.EventContract)!.Properties.Select(property => fact.Values.FirstOrDefault(value => value.TargetProperty == property)?.Value)
        .Where(value => value is not null and not SemanticNullValue).Select(value => value!)];

    static SemanticConstraintTarget? Target(SemanticConstraint constraint, SemanticId id) => constraint.Targets.FirstOrDefault(target => target.EventContract == id);

    static bool Equal(SemanticValue left, SemanticValue right) => (left, right) switch
    {
        (SemanticNullValue, SemanticNullValue) => true,
        (SemanticTextValue a, SemanticTextValue b) => a.Value == b.Value,
        (SemanticNumberValue a, SemanticNumberValue b) => a.Value == b.Value,
        (SemanticBooleanValue a, SemanticBooleanValue b) => a.Value == b.Value,
        (SemanticArrayValue a, SemanticArrayValue b) => a.Values.Length == b.Values.Length && a.Values.Zip(b.Values).All(pair => Equal(pair.First, pair.Second)),
        (SemanticCompositeValue a, SemanticCompositeValue b) => a.Properties.Length == b.Properties.Length && a.Properties.All(property => b.Properties.Any(other => other.TargetProperty == property.TargetProperty && Equal(other.Value, property.Value))),
        _ => false
    };
}
