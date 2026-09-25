// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Rejects authorization that cannot be rendered safely.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    internal static bool IsRoleClaim(string type) => string.Equals(type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase);

    static bool ValidateCommandAuthorization(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // The reference evaluator selects the first supplied identifier, not necessarily the first declared
        // identifier. A generated command record cannot distinguish omitted values from defaulted values.
        if (command.Authorization is not null && command.Properties.Count(property => property.IsIdentifier) != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorized command '{command.Name}' needs exactly one identifier to preserve the reference subject.", command.Id));
            return false;
        }

        return ValidateAuthorization(context, command.Authorization, command.Id, command.Name, diagnostics) &&
            ValidateClaimTargets(context, command.Authorization, command.Properties, command.Properties.SingleOrDefault(property => property.IsIdentifier), command.Id, command.Name, diagnostics);
    }

    static bool ValidateQueryAuthorization(SemanticApplicationContext context, SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics) =>
        ValidateAuthorization(context, query.Authorization, query.Id, query.Name, diagnostics) &&
        ValidateClaimTargets(
            context,
            query.Authorization,
            [new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)],
            new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false),
            query.Id,
            query.Name,
            diagnostics);

    static bool ValidateAuthorization(SemanticApplicationContext context, SemanticAuthorization? authorization, SemanticId id, string name, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        // Arc's authorization context does not supply the received-at occurrence of PolicyContext v1.
        // Never emit a policy with a fabricated occurrence or a predicate that silently denies.
        var opaque = OpaquePolicies(authorization, context.Application.Policies).FirstOrDefault();
        if (opaque is not null)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' references opaque policy '{opaque}'; Stage cannot render PolicyContext.Occurred (the received-at time) at Arc's authorization boundary.", id));
            return false;
        }

        // Arc's Authorize attribute requires an authenticated principal even when its named policy would
        // accept an unauthenticated caller. Admit only expressions that already require authentication.
        if (CanRender(authorization, context.Application.Policies) && RequiresAuthentication(authorization, context.Application.Policies) &&
            !Claims(authorization, context.Application.Policies).Any(claim => IsRoleClaim(claim.Claim)))
        {
            return true;
        }

        diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' cannot be rendered exactly with Arc's authenticated policy boundary, contains a role claim URI, or has an unresolved or non-portable policy.", id));
        return false;
    }

    static bool ValidateClaimTargets(
        SemanticApplicationContext context,
        SemanticAuthorization? authorization,
        IReadOnlyList<SemanticProperty> properties,
        SemanticProperty? subject,
        SemanticId id,
        string name,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        var paths = Claims(authorization, context.Application.Policies).Where(claim => claim.TargetKind != SemanticClaimTargetKind.Literal)
            .Select(claim => claim.TargetKind == SemanticClaimTargetKind.Subject ? subject?.Name : claim.Value);
        if (paths.All(path => path is not null && IsTextPath(context, properties, path)))
        {
            return true;
        }

        diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' compares a claim with a non-text artifact value.", id));
        return false;
    }

    static IEnumerable<string> OpaquePolicies(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => policies.SingleOrDefault(policy => policy.Name == reference.Name)?.Condition is SemanticOpaquePolicyCondition ? [reference.Name] : [],
        SemanticLogicalAuthorization logical => OpaquePolicies(logical.Left, policies).Concat(OpaquePolicies(logical.Right, policies)),
        _ => []
    };

    static IEnumerable<SemanticClaimCondition> Claims(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => Conditions(policies.Single(policy => policy.Name == reference.Name).Condition),
        SemanticLogicalAuthorization logical => Claims(logical.Left, policies).Concat(Claims(logical.Right, policies)),
        _ => []
    };

    static IEnumerable<SemanticClaimCondition> Conditions(SemanticPolicyCondition condition) => condition switch
    {
        SemanticClaimCondition claim => [claim],
        SemanticLogicalPolicyCondition logical => Conditions(logical.Left).Concat(Conditions(logical.Right)),
        _ => []
    };

    static bool IsTextPath(SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string path)
    {
        SemanticProperty? property = null;
        foreach (var segment in path.Split('.'))
        {
            property = properties.SingleOrDefault(candidate => candidate.Name == segment);
            if (property is null)
            {
                return false;
            }

            properties = property.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(property.Type.Target, out var composite)
                ? composite.Properties : [];
        }

        return property is { Type.IsCollection: false } && (property.Type.Kind == SemanticTypeReferenceKind.Primitive
            ? property.Type.Primitive == SemanticPrimitiveType.Text
            : property.Type.Kind == SemanticTypeReferenceKind.Concept && context.Concepts[property.Type.Target].Primitive == SemanticPrimitiveType.Text);
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
