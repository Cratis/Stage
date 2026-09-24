// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Rejects authorization that cannot be rendered safely.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static bool ValidateCommandAuthorization(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // The reference evaluator selects the first supplied identifier, not necessarily the first declared
        // identifier. A generated command record cannot distinguish omitted values from defaulted values.
        if (command.Authorization is not null && command.Properties.Count(property => property.IsIdentifier) != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorized command '{command.Name}' needs exactly one identifier to preserve the reference subject.", command.Id));
            return false;
        }

        return ValidateAuthorization(context, command.Authorization, command.Id, command.Name, diagnostics);
    }

    static bool ValidateQueryAuthorization(SemanticApplicationContext context, SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics) =>
        ValidateAuthorization(context, query.Authorization, query.Id, query.Name, diagnostics);

    static bool ValidateAuthorization(SemanticApplicationContext context, SemanticAuthorization? authorization, SemanticId id, string name, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        // Arc's Authorize attribute requires an authenticated principal even when its named policy would
        // accept an unauthenticated caller. Admit only expressions that already require authentication.
        if (CanRender(authorization, context.Application.Policies) && RequiresAuthentication(authorization, context.Application.Policies))
        {
            return true;
        }

        diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' cannot be rendered exactly with Arc's authenticated policy boundary, or contains an unresolved or non-portable policy.", id));
        return false;
    }

    static bool RequiresAuthentication(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => policies.SingleOrDefault(policy => policy.Name == reference.Name) is { } policy && RequiresAuthentication(policy.Condition),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical =>
            RequiresAuthentication(logical.Left, policies) || RequiresAuthentication(logical.Right, policies),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical =>
            RequiresAuthentication(logical.Left, policies) && RequiresAuthentication(logical.Right, policies),
        _ => false
    };

    static bool RequiresAuthentication(SemanticPolicyCondition condition) => condition switch
    {
        SemanticAuthenticatedCondition => true,
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical =>
            RequiresAuthentication(logical.Left) || RequiresAuthentication(logical.Right),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical =>
            RequiresAuthentication(logical.Left) && RequiresAuthentication(logical.Right),
        _ => false
    };

    static bool CanRender(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => policies.SingleOrDefault(policy => policy.Name == reference.Name) is { } policy && CanRender(policy.Condition),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And or SemanticLogicalOperator.Or } logical =>
            CanRender(logical.Left, policies) && CanRender(logical.Right, policies),
        _ => false
    };

    static bool CanRender(SemanticPolicyCondition condition) => condition switch
    {
        SemanticAuthenticatedCondition => true,
        SemanticRoleCondition { Role: not null } => true,
        SemanticClaimCondition { Claim: not null, TargetKind: SemanticClaimTargetKind.Subject, Value: null } => true,
        SemanticClaimCondition { Claim: not null, TargetKind: SemanticClaimTargetKind.Literal or SemanticClaimTargetKind.Artifact, Value: not null } => true,
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And or SemanticLogicalOperator.Or } logical =>
            CanRender(logical.Left) && CanRender(logical.Right),
        _ => false
    };
}
