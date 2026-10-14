// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications;

// A formatted run record is never used to infer whether a failure is catchable.
internal sealed record SemanticInvocationOutcome(SemanticSpecificationRunRecord? Failure, SemanticInvocationRefusalOutcome? Refusal = null, CommandResult? CommandResult = null);

internal sealed record SemanticInvocationRefusalOutcome(string Reason, IReadOnlyList<SemanticConstraintFailure> Constraints, string Message)
{
    internal bool Matches(SemanticInvocationRefusal branch) => branch.Selector switch
    {
        "any" => Reason == "validation" || Reason == "constraint",
        "constraint" => Reason == "constraint" && (branch.Constraint is null || Constraints.Any(failure => failure.Name == branch.Constraint)),
        _ => branch.Selector == Reason
    };

    internal SemanticValue ValueOf(string member) => SemanticValue.Text(member switch
    {
        "reason" => Reason,
        "constraint" when Constraints.Count > 0 => Constraints[0].Name,
        "message" => Message,
        _ => throw new InvalidSemanticContract("A refusal expression has an unavailable member.")
    });
}
