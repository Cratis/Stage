// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

// Stage-owned port of Screenplay v4.204.0 SemanticReactionBatchClaims (decision 0078): persisted property
// claims and batch reservations are independent. A later value change or release cannot free a reservation,
// whereas event-occurrence cycles can release. Do not replace this with SemanticConstraintEvaluator.FindViolation,
// which replaces an owner's claim on a value change.
internal sealed class SemanticReactionBatchClaims(SemanticExecutionPlan plan, IReadOnlyList<SemanticConstraintEvaluator.Fact> history)
{
    readonly SemanticConstraint[] _constraints = [.. plan.Constraints.Values.OrderBy(constraint => constraint.Name, StringComparer.Ordinal)];
    readonly Dictionary<string, List<(SemanticValue Owner, SemanticValue[] Value)>> _reservations = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<(SemanticValue Owner, bool Open)>> _cycles = new(StringComparer.Ordinal);

    internal SemanticConstraint? Validate(SemanticConstraintEvaluator.Fact candidate)
    {
        foreach (var constraint in _constraints.Where(constraint => SemanticConstraintEvaluator.Target(constraint, candidate.Contract) is not null))
        {
            var violated = constraint.Kind switch
            {
                SemanticConstraintKind.UniquePropertyValue => ViolatesProperty(constraint, candidate),
                SemanticConstraintKind.UniqueEventOccurrence => CycleIsOpen(constraint, candidate),
                _ => throw new UnsupportedSemanticMapping()
            };
            if (violated) return constraint;
        }

        // Transitions from a rejected candidate must never affect a subsequent candidate.
        foreach (var constraint in _constraints)
        {
            var targeted = SemanticConstraintEvaluator.Target(constraint, candidate.Contract) is not null;
            if (constraint.Kind == SemanticConstraintKind.UniquePropertyValue && targeted)
            {
                var value = SemanticConstraintEvaluator.Constrained(constraint, candidate);
                if (value.Length > 0) Reservations(constraint).Add((candidate.Destination, value));
            }
            else if (constraint.Kind == SemanticConstraintKind.UniqueEventOccurrence && (targeted || constraint.ReleasedBy.Contains(candidate.Contract)))
            {
                var cycles = Cycles(constraint);
                cycles.RemoveAll(claim => SemanticExpectationComparer.AreEqual(claim.Owner, candidate.Destination));
                cycles.Add((candidate.Destination, !constraint.ReleasedBy.Contains(candidate.Contract)));
            }
        }

        return null;
    }

    bool ViolatesProperty(SemanticConstraint constraint, SemanticConstraintEvaluator.Fact candidate)
    {
        if (SemanticConstraintEvaluator.ViolatesValue(constraint, candidate, history)) return true;
        var value = SemanticConstraintEvaluator.Constrained(constraint, candidate);

        return value.Length > 0 && Reservations(constraint).Exists(claim =>
            !SemanticExpectationComparer.AreEqual(claim.Owner, candidate.Destination) &&
            SemanticConstraintEvaluator.SameValue(claim.Value, value, constraint.IgnoreCasing));
    }

    bool CycleIsOpen(SemanticConstraint constraint, SemanticConstraintEvaluator.Fact candidate)
    {
        var staged = Cycles(constraint).Where(claim => SemanticExpectationComparer.AreEqual(claim.Owner, candidate.Destination)).ToArray();

        return staged.Length > 0 ? staged[0].Open : SemanticConstraintEvaluator.ViolatesOccurrence(constraint, candidate, history);
    }

    List<(SemanticValue Owner, SemanticValue[] Value)> Reservations(SemanticConstraint constraint)
    {
        if (!_reservations.TryGetValue(constraint.Name, out var claims)) _reservations.Add(constraint.Name, claims = []);

        return claims;
    }

    List<(SemanticValue Owner, bool Open)> Cycles(SemanticConstraint constraint)
    {
        if (!_cycles.TryGetValue(constraint.Name, out var claims)) _cycles.Add(constraint.Name, claims = []);

        return claims;
    }
}
