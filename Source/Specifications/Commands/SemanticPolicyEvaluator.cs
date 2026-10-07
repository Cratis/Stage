// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticPolicyEvaluator
{
    internal static ClaimsPrincipal Principal(SemanticCaller caller)
    {
        const string roleClaim = ClaimTypes.Role;
        var claims = caller.Roles.Select(role => new Claim(roleClaim, role))
            .Concat(caller.Claims.Select(claim => new Claim(claim.Type, claim.Value)));
        return new(new ClaimsIdentity(claims, caller.Authenticated ? "stage" : null, ClaimTypes.Name, roleClaim));
    }

    internal static bool Allows(SemanticAuthorization? authorization, SemanticExecutionPlan plan, SemanticCaller? caller, ClaimsPrincipal principal, SemanticCommand command, SemanticSpecification specification)
    {
        if (authorization is null) return true;
        if (caller is not { Roles.IsDefault: false, Claims.IsDefault: false }) return false;
        var values = specification.When!.Values;
        var artifact = command.Properties.Select(property => (property, value: values.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value))
            .Where(pair => pair.value is not null).ToDictionary(pair => pair.property.Name, pair => pair.value!, StringComparer.Ordinal);
        var subject = command.Properties.Where(property => property.IsIdentifier)
            .Select(property => values.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value).FirstOrDefault(value => value is not null);
        return Authorization(authorization, plan, caller, principal, artifact, subject, command.Properties);
    }

    static bool Authorization(SemanticAuthorization authorization, SemanticExecutionPlan plan, SemanticCaller caller, ClaimsPrincipal principal, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties) => authorization switch
    {
        SemanticPolicyReference reference => plan.Model.Application.Policies.Single(policy => policy.Name == reference.Name).Condition is SemanticOpaquePolicyCondition
            ? throw new OpaquePolicyReached(reference.Name)
            : Condition(plan.Model.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, plan, caller, principal, artifact, subject, properties),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => Authorization(logical.Left, plan, caller, principal, artifact, subject, properties) && Authorization(logical.Right, plan, caller, principal, artifact, subject, properties),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => Authorization(logical.Left, plan, caller, principal, artifact, subject, properties) || Authorization(logical.Right, plan, caller, principal, artifact, subject, properties),
        _ => throw new UnsupportedSemanticMapping()
    };

    static bool Condition(SemanticPolicyCondition condition, SemanticExecutionPlan plan, SemanticCaller caller, ClaimsPrincipal principal, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties) => condition switch
    {
        SemanticAuthenticatedCondition => principal.Identity?.IsAuthenticated == true,
        SemanticRoleCondition role => principal.IsInRole(role.Role),
        SemanticClaimCondition claim => ClaimMatches(claim, plan, caller, artifact, subject, properties),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => Condition(logical.Left, plan, caller, principal, artifact, subject, properties) && Condition(logical.Right, plan, caller, principal, artifact, subject, properties),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => Condition(logical.Left, plan, caller, principal, artifact, subject, properties) || Condition(logical.Right, plan, caller, principal, artifact, subject, properties),
        _ => throw new UnsupportedSemanticMapping()
    };

    static bool ClaimMatches(SemanticClaimCondition claim, SemanticExecutionPlan plan, SemanticCaller caller, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties)
    {
        if (claim.TargetKind == SemanticClaimTargetKind.Literal)
        {
            return claim.Value is not null && caller.Claims.Any(value =>
                string.Equals(value.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(value.Value, claim.Value, StringComparison.Ordinal));
        }

        var (target, type) = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Subject => (subject, properties.FirstOrDefault(property => property.IsIdentifier && artifact.ContainsKey(property.Name))?.Type),
            SemanticClaimTargetKind.Artifact when claim.Value is not null => ArtifactValue(claim.Value, artifact, plan, properties),
            _ => (null, null)
        };
        if (target is not SemanticTextValue text || type is not { IsCollection: false }) return false;
        var primitive = type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive,
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            _ => (SemanticPrimitiveType?)null
        };
        return caller.Claims.Any(value => string.Equals(value.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) &&
            (primitive == SemanticPrimitiveType.Uuid
                ? Guid.TryParse(text.Value, out var subjectId) && Guid.TryParse(value.Value, out var claimId) && subjectId == claimId
                : primitive == SemanticPrimitiveType.Text && string.Equals(value.Value, text.Value, StringComparison.Ordinal)));
    }

    static (SemanticValue? Value, SemanticTypeReference? Type) ArtifactValue(string path, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticExecutionPlan plan, IEnumerable<SemanticProperty> properties)
    {
        var parts = path.Split('.');
        if (!artifact.TryGetValue(parts[0], out var value)) return (null, null);
        var type = properties.Single(property => property.Name == parts[0]).Type;
        foreach (var name in parts.Skip(1))
        {
            if (value is not SemanticCompositeValue composite || type.Kind != SemanticTypeReferenceKind.CompositeType) return (null, null);
            var shape = plan.Model.Application.Types.Single(declaration => declaration.Id == type.Target);
            var member = shape.Properties.Single(property => property.Name == name);
            value = composite.Properties.SingleOrDefault(property => property.TargetProperty == member.Id)?.Value;
            if (value is null) return (null, null);
            type = member.Type;
        }

        return (value, type);
    }
}

// A private control signal must not become part of the Specifications assembly's public exception API.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "The authorization signal is internal to the specification executor.")]
internal sealed class OpaquePolicyReached(string policy) : Exception($"Policy '{policy}' requires a target to evaluate its implementation body.");
