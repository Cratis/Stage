// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Host.Published;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Host.Export;

/// <summary>
/// Derives the sign-in requirements from the policies the model applies to its operations.
/// </summary>
internal static class AuthenticationRequirementsReader
{
    internal static AuthenticationRequirements From(ExecutableSemanticModel model)
    {
        var policies = model.Application.Policies;
        var applied = SemanticHostModelWalker.Slices(model)
            .SelectMany(located => located.Slice.Commands.Select(command => command.Authorization)
                .Concat(located.Slice.Queries.Select(query => query.Authorization)))
            .OfType<SemanticAuthorization>()
            .Where(authorization => SemanticAnonymousAccess.RequiresCaller(authorization, policies))
            .ToArray();

        var names = applied.SelectMany(Names).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var roles = new SortedSet<string>(StringComparer.Ordinal);
        var claims = new SortedSet<(string Name, string? Value)>(Comparer<(string Name, string? Value)>.Create(
            (left, right) => (StringComparer.Ordinal.Compare(left.Name, right.Name), StringComparer.Ordinal.Compare(left.Value, right.Value)) switch
            {
                (0, var value) => value,
                (var name, _) => name
            }));
        var opaque = false;
        foreach (var name in names)
        {
            if (policies.FirstOrDefault(policy => policy.Name == name)?.Condition is { } condition)
            {
                Collect(condition, roles, claims, ref opaque);
            }
        }

        return new AuthenticationRequirements(
            applied.Length > 0,
            names,
            [.. roles],
            [.. claims.Select(claim => new ClaimRequirement(claim.Name, claim.Value))],
            opaque);
    }

    static IEnumerable<string> Names(SemanticAuthorization authorization) => authorization switch
    {
        SemanticPolicyReference reference => [reference.Name],
        SemanticLogicalAuthorization logical => Names(logical.Left).Concat(Names(logical.Right)),
        _ => []
    };

    static void Collect(SemanticPolicyCondition condition, SortedSet<string> roles, SortedSet<(string Name, string? Value)> claims, ref bool opaque)
    {
        switch (condition)
        {
            case SemanticRoleCondition role:
                roles.Add(role.Role);
                break;
            case SemanticClaimCondition claim:
                claims.Add((claim.Claim, claim.Value));
                break;
            case SemanticOpaquePolicyCondition:
                opaque = true;
                break;
            case SemanticNotPolicyCondition not:
                Collect(not.Operand, roles, claims, ref opaque);
                break;
            case SemanticLogicalPolicyCondition logical:
                Collect(logical.Left, roles, claims, ref opaque);
                Collect(logical.Right, roles, claims, ref opaque);
                break;
        }
    }
}
