// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Policies;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>policy</c> declarations of an application into Stage
/// <see cref="PolicyDefinition"/> records.
/// </summary>
/// <remarks>
/// <see cref="AuthorizationConverter"/> carries the names a command requires; this carries what those names
/// mean. Without both, a consumer can say a command is guarded by <c>IsAccountant</c> and cannot say what
/// <c>IsAccountant</c> checks.
/// </remarks>
public static class PolicyConverter
{
    /// <summary>
    /// Converts an application's policy declarations into their Stage records.
    /// </summary>
    /// <param name="policies">The policy declarations.</param>
    /// <param name="modelName">The name of the event model, used to derive stable identifiers.</param>
    /// <returns>The Stage policy definitions, in declaration order.</returns>
    public static IReadOnlyList<PolicyDefinition> Convert(IEnumerable<PolicySyntax> policies, string modelName) =>
    [
        .. policies.Select(policy => new PolicyDefinition(
            DeterministicId.From($"model:{modelName}:policy:{policy.Name}"),
            policy.Name,
            Convert(policy.Condition),
            policy.Code?.Language ?? string.Empty))
    ];

    static PolicyCondition? Convert(PolicyConditionSyntax? condition) =>
        condition switch
        {
            null => null,
            AuthenticatedConditionSyntax => new AuthenticatedCondition(),
            RoleConditionSyntax role => new RoleCondition(role.Role),
            ClaimConditionSyntax claim => Claim(claim),
            LogicalPolicyConditionSyntax logical when Convert(logical.Left) is { } left && Convert(logical.Right) is { } right =>
                new LogicalPolicyCondition(left, ConditionConverter.Operator(logical.Operator), right),
            _ => null
        };

    // A claim matched against the subject states no target of its own, so there is no value to carry and none
    // is invented - MatchesSubject is what says where the value comes from in that case.
    static ClaimCondition Claim(ClaimConditionSyntax claim)
    {
        var (kind, expression) = claim.Matches is null
            ? (ProducedValueKind.Unsupported, string.Empty)
            : ProducedValueConverter.Convert(claim.Matches);

        return new ClaimCondition(claim.Claim, claim.MatchesSubject, kind, expression);
    }
}
