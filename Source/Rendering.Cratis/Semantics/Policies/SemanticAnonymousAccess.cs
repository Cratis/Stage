// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Recognizes the sole policy reference that grants access independently of a caller.
/// </summary>
internal static class SemanticAnonymousAccess
{
    internal static bool IsAnonymous(SemanticAuthorization? authorization, IEnumerable<SemanticPolicy> policies) =>
        authorization is SemanticPolicyReference reference &&
        policies.SingleOrDefault(policy => policy.Name == reference.Name)?.Condition is SemanticAnonymousCondition;

    internal static bool RequiresCaller(SemanticAuthorization? authorization, IEnumerable<SemanticPolicy> policies) =>
        authorization is not null && !IsAnonymous(authorization, policies);

    internal static bool NestsAnonymous(SemanticAuthorization? authorization, IEnumerable<SemanticPolicy> policies) =>
        !IsAnonymous(authorization, policies) && ContainsAnonymous(authorization, policies);

    static bool ContainsAnonymous(SemanticAuthorization? authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => policies.SingleOrDefault(policy => policy.Name == reference.Name) is { } policy && ContainsAnonymous(policy.Condition),
        SemanticLogicalAuthorization logical => ContainsAnonymous(logical.Left, policies) || ContainsAnonymous(logical.Right, policies),
        _ => false
    };

    static bool ContainsAnonymous(SemanticPolicyCondition condition) => condition switch
    {
        SemanticAnonymousCondition => true,
        SemanticNotPolicyCondition not => ContainsAnonymous(not.Operand),
        SemanticLogicalPolicyCondition logical => ContainsAnonymous(logical.Left) || ContainsAnonymous(logical.Right),
        _ => false
    };
}
