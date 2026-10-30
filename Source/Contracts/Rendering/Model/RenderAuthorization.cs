// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal abstract record RenderAuthorization : RenderMember
{
    RenderAuthorization()
    {
    }
    internal sealed record Authenticated : RenderAuthorization;
    internal sealed record Anonymous : RenderAuthorization;
    internal sealed record Role(string Name) : RenderAuthorization;
    internal sealed record Claim(string Name, SemanticClaimTargetKind TargetKind, string? Value) : RenderAuthorization;
    internal sealed record Policy(string Name, RenderAuthorization Condition) : RenderAuthorization;
    internal sealed record OpaquePolicy(string RequirementId) : RenderAuthorization;
    internal sealed record NotPolicy(RenderAuthorization Operand) : RenderAuthorization;
    internal sealed record Logical(RenderAuthorization Left, SemanticLogicalOperator Operator, RenderAuthorization Right) : RenderAuthorization;
}

internal sealed class RenderAuthorizationNormalizer(SemanticApplication application)
{
    internal RenderAuthorization Normalize(SemanticAuthorization authorization) => authorization switch
    {
        SemanticPolicyReference value => new RenderAuthorization.Policy(value.Name, Policy((application.Policies.FirstOrDefault(policy => StringComparer.Ordinal.Equals(policy.Name, value.Name)) ?? throw new InvalidArtifactRenderContract($"Missing policy declaration '{value.Name}'.")).Condition)),
        SemanticLogicalAuthorization value when Enum.IsDefined(value.Operator) => new RenderAuthorization.Logical(Normalize(value.Left), value.Operator, Normalize(value.Right)),
        _ => throw new InvalidArtifactRenderContract($"Unclassified authorization '{authorization.GetType().Name}'.")
    };

    RenderAuthorization Policy(SemanticPolicyCondition condition) => condition switch
    {
        SemanticAuthenticatedCondition => new RenderAuthorization.Authenticated(),
        SemanticAnonymousCondition => new RenderAuthorization.Anonymous(),
        SemanticRoleCondition value => new RenderAuthorization.Role(value.Role),
        SemanticClaimCondition value => new RenderAuthorization.Claim(value.Claim, value.TargetKind, value.Value),
        SemanticOpaquePolicyCondition value => new RenderAuthorization.OpaquePolicy(value.RequirementId),
        SemanticNotPolicyCondition value => new RenderAuthorization.NotPolicy(Policy(value.Operand)),
        SemanticLogicalPolicyCondition value when Enum.IsDefined(value.Operator) => new RenderAuthorization.Logical(Policy(value.Left), value.Operator, Policy(value.Right)),
        _ => throw new InvalidArtifactRenderContract($"Unclassified policy condition '{condition.GetType().Name}'.")
    };
}
