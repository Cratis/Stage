// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    // Generated scenarios execute the admitted policy in Arc; the authored then block is their oracle.
    static string? OpaquePolicyRejectionReason(SemanticApplicationContext context, SemanticSpecification specification)
    {
        var operations = specification.ThenQueries.Select(expected => expected.Query);
        if (specification.When is { } action) operations = operations.Append(action.Command);
        return operations.Select(operation => OperationPolicyRejectionReason(context, specification, operation))
            .FirstOrDefault(reason => reason is not null);
    }

    static string? OperationPolicyRejectionReason(SemanticApplicationContext context, SemanticSpecification specification, SemanticId operation)
    {
        var authorization = context.Queries.TryGetValue(operation, out var query) ? query.Authorization : null;
        if (context.Commands.TryGetValue(operation, out var command)) authorization = command.Authorization;
        return authorization is null ? null : SemanticCratisAdmission.OpaquePolicies(authorization, context.Application.Policies)
            .Select(policy => PolicyContextRejectionReason(context, specification, operation, policy))
            .FirstOrDefault(reason => reason is not null);
    }

    static string? PolicyContextRejectionReason(SemanticApplicationContext context, SemanticSpecification specification, SemanticId operation, SemanticPolicy policy)
    {
        if (policy.Condition is not SemanticOpaquePolicyCondition opaque ||
            !context.PolicyContextReads.TryGetValue((opaque.RequirementId, operation), out var reads))
        {
            return "The operation's opaque csharp/file policy has no admitted context-read analysis.";
        }
        if (reads.Contains("Occurred") && !HasDeterministicPolicyTime(specification, operation))
        {
            return "An opaque policy reads context.Occurred; only a command scenario with an authored given clock supplies a deterministic authorization receipt time.";
        }

        return null;
    }

    static bool HasDeterministicPolicyTime(SemanticSpecification specification, SemanticId operation) =>
        specification.When?.Command == operation && SemanticSpecificationClock.TryGet(specification, out _);
}
